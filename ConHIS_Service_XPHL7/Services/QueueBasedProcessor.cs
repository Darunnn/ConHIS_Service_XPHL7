using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ConHIS_Service_XPHL7.Services
{
    /// <summary>
    /// Queue-based processor ที่ป้องกันการข้ามข้อมูลแม้ Interval จะเร็ว
    /// ปรับปรุงโครงสร้างให้มีความชัดเจนและ maintainable มากขึ้น
    /// </summary>
    public class QueueBasedProcessor
    {
        #region Fields

        // Queues สำหรับเก็บข้อมูล pending
        private readonly ConcurrentQueue<DrugDispenseipd> _ipdQueue = new ConcurrentQueue<DrugDispenseipd>();
        private readonly ConcurrentQueue<DrugDispenseopd> _opdQueue = new ConcurrentQueue<DrugDispenseopd>();

        // Semaphores สำหรับควบคุม concurrency
        private readonly SemaphoreSlim _ipdSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _opdSemaphore = new SemaphoreSlim(1, 1);

        // Services
        private readonly DatabaseService _databaseService;
        private readonly DrugDispenseProcessor _processor;
        private readonly LogManager _logger;

        // Timers สำหรับ IPD
        private Timer _ipdFetchTimer;
        private Timer _ipdProcessTimer;

        // Timers สำหรับ OPD
        private Timer _opdFetchTimer;
        private Timer _opdProcessTimer;

        // Flags สำหรับป้องกัน concurrent execution
        private volatile bool _isIPDFetching = false;
        private volatile bool _isIPDProcessing = false;
        private volatile bool _isOPDFetching = false;
        private volatile bool _isOPDProcessing = false;

        // Default intervals (in seconds)
        private const int DEFAULT_FETCH_INTERVAL = 5;
        private const int DEFAULT_PROCESS_INTERVAL = 1;
        private const int INITIAL_PROCESS_DELAY = 500; // milliseconds

        #endregion

        #region Constructor

        public QueueBasedProcessor(
            DatabaseService databaseService,
            DrugDispenseProcessor processor,
            LogManager logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods - Service Control

        /// <summary>
        /// เริ่มต้น IPD Service
        /// </summary>
        public void StartIPDService(int fetchIntervalSeconds = DEFAULT_FETCH_INTERVAL,
                                     int processIntervalSeconds = DEFAULT_PROCESS_INTERVAL)
        {
            _logger.LogInfo($"Starting IPD Queue Service - Fetch: {fetchIntervalSeconds}s, Process: {processIntervalSeconds}s");

            // Timer 1: ดึงข้อมูลใหม่เข้า Queue
            _ipdFetchTimer = new Timer(
                async _ => await FetchIPDDataAsync(),
                null,
                0, // เริ่มทันที
                fetchIntervalSeconds * 1000
            );

            // Timer 2: ประมวลผลข้อมูลใน Queue
            _ipdProcessTimer = new Timer(
                async _ => await ProcessIPDQueueAsync(),
                null,
                INITIAL_PROCESS_DELAY, // เริ่มหลัง 0.5 วินาที
                processIntervalSeconds * 1000
            );
        }

        /// <summary>
        /// เริ่มต้น OPD Service
        /// </summary>
        public void StartOPDService(int fetchIntervalSeconds = DEFAULT_FETCH_INTERVAL,
                                     int processIntervalSeconds = DEFAULT_PROCESS_INTERVAL)
        {
            _logger.LogInfo($"Starting OPD Queue Service - Fetch: {fetchIntervalSeconds}s, Process: {processIntervalSeconds}s");

            // Timer 1: ดึงข้อมูลใหม่เข้า Queue
            _opdFetchTimer = new Timer(
                async _ => await FetchOPDDataAsync(),
                null,
                0, // เริ่มทันที
                fetchIntervalSeconds * 1000
            );

            // Timer 2: ประมวลผลข้อมูลใน Queue
            _opdProcessTimer = new Timer(
                async _ => await ProcessOPDQueueAsync(),
                null,
                INITIAL_PROCESS_DELAY, // เริ่มหลัง 0.5 วินาที
                processIntervalSeconds * 1000
            );
        }

        /// <summary>
        /// หยุด IPD Service
        /// </summary>
        public void StopIPDService()
        {
            _ipdFetchTimer?.Dispose();
            _ipdProcessTimer?.Dispose();
            _ipdFetchTimer = null;
            _ipdProcessTimer = null;

            _logger.LogInfo($"IPD Service stopped - Remaining queue: {_ipdQueue.Count}");
        }

        /// <summary>
        /// หยุด OPD Service
        /// </summary>
        public void StopOPDService()
        {
            _opdFetchTimer?.Dispose();
            _opdProcessTimer?.Dispose();
            _opdFetchTimer = null;
            _opdProcessTimer = null;

            _logger.LogInfo($"OPD Service stopped - Remaining queue: {_opdQueue.Count}");
        }

        /// <summary>
        /// ดึงสถานะของ Queue
        /// </summary>
        public string GetQueueStatus()
        {
            return $"IPD Queue: {_ipdQueue.Count} | OPD Queue: {_opdQueue.Count}";
        }

        /// <summary>
        /// ล้างข้อมูลใน Queue ทั้งหมด
        /// </summary>
        public void ClearAllQueues()
        {
            while (_ipdQueue.TryDequeue(out _)) { }
            while (_opdQueue.TryDequeue(out _)) { }
            _logger.LogInfo("All queues cleared");
        }

        #endregion

        #region IPD Processing

        /// <summary>
        /// ดึงข้อมูล IPD ใหม่จาก Database เข้า Queue
        /// </summary>
        private async Task FetchIPDDataAsync()
        {
            // ป้องกันการทำงานซ้ำซ้อน
            if (_isIPDFetching)
            {
                _logger.LogInfo("[IPD Fetch] Already fetching, skipping this cycle");
                return;
            }

            _isIPDFetching = true;

            try
            {
                await _ipdSemaphore.WaitAsync();

                var pendingData = await Task.Run(() =>
                    _databaseService.GetPendingDispenseDataByOrderType("IPD")
                );

                if (pendingData == null || pendingData.Count == 0)
                {
                    return;
                }

                int added = 0;
                int duplicates = 0;

                foreach (var data in pendingData)
                {
                    // ตรวจสอบว่ามีใน Queue แล้วหรือไม่
                    if (!IsInIPDQueue(data.DrugDispenseipdId))
                    {
                        _ipdQueue.Enqueue(data);
                        added++;
                    }
                    else
                    {
                        duplicates++;
                    }
                }

                if (added > 0)
                {
                    _logger.LogInfo($"[IPD Fetch] Added {added} new items to queue (Total: {_ipdQueue.Count}, Duplicates: {duplicates})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[IPD Fetch] Error fetching data", ex);
            }
            finally
            {
                _ipdSemaphore.Release();
                _isIPDFetching = false;
            }
        }

        /// <summary>
        /// ประมวลผลข้อมูลใน IPD Queue
        /// </summary>
        private async Task ProcessIPDQueueAsync()
        {
            // ป้องกันการทำงานซ้ำซ้อน
            if (_isIPDProcessing)
            {
                return;
            }

            // ตรวจสอบว่า Queue ว่างหรือไม่
            if (_ipdQueue.IsEmpty)
            {
                return;
            }

            _isIPDProcessing = true;

            try
            {
                // ประมวลผลครั้งละ 1 รายการ
                if (_ipdQueue.TryDequeue(out var data))
                {
                    _logger.LogInfo($"[IPD Process] Processing ID: {data.DrugDispenseipdId} (Remaining: {_ipdQueue.Count})");

                    await Task.Run(() =>
                    {
                        _processor.ProcessSingleOrder(
                            data,
                            "IPD",
                            result =>
                            {
                                if (result.Success)
                                {
                                    _logger.LogInfo($"[IPD Process] ✓ Success ID: {data.DrugDispenseipdId}");
                                }
                                else
                                {
                                    _logger.LogWarning($"[IPD Process] ✗ Failed ID: {data.DrugDispenseipdId} - {result.Message}");
                                }
                            }
                        );
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[IPD Process] Error processing queue item", ex);
            }
            finally
            {
                _isIPDProcessing = false;
            }
        }

        /// <summary>
        /// ตรวจสอบว่า ID มีอยู่ใน IPD Queue แล้วหรือไม่
        /// </summary>
        private bool IsInIPDQueue(int id)
        {
            foreach (var item in _ipdQueue)
            {
                if (item.DrugDispenseipdId == id)
                    return true;
            }
            return false;
        }

        #endregion

        #region OPD Processing

        /// <summary>
        /// ดึงข้อมูล OPD ใหม่จาก Database เข้า Queue
        /// </summary>
        private async Task FetchOPDDataAsync()
        {
            // ป้องกันการทำงานซ้ำซ้อน
            if (_isOPDFetching)
            {
                _logger.LogInfo("[OPD Fetch] Already fetching, skipping this cycle");
                return;
            }

            _isOPDFetching = true;

            try
            {
                await _opdSemaphore.WaitAsync();

                var pendingData = await Task.Run(() =>
                    _databaseService.GetPendingDispenseDataByOrderType("OPD")
                );

                if (pendingData == null || pendingData.Count == 0)
                {
                    return;
                }

                int added = 0;
                int duplicates = 0;

                foreach (var data in pendingData)
                {
                    // ตรวจสอบว่ามีใน Queue แล้วหรือไม่
                    if (!IsInOPDQueue(data.DrugDispenseipdId))
                    {
                        // แปลง DrugDispenseipd เป็น DrugDispenseopd
                        var opdData = new DrugDispenseopd
                        {
                            DrugDispenseopdId = data.DrugDispenseipdId,
                            PrescId = data.PrescId,
                            DrugRequestMsgType = data.DrugRequestMsgType,
                            Hl7Data = data.Hl7Data,
                            DrugDispenseDatetime = data.DrugDispenseDatetime,
                            RecieveStatus = data.RecieveStatus,
                            RecieveStatusDatetime = data.RecieveStatusDatetime,
                            RecieveOrderType = data.RecieveOrderType
                        };

                        _opdQueue.Enqueue(opdData);
                        added++;
                    }
                    else
                    {
                        duplicates++;
                    }
                }

                if (added > 0)
                {
                    _logger.LogInfo($"[OPD Fetch] Added {added} new items to queue (Total: {_opdQueue.Count}, Duplicates: {duplicates})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[OPD Fetch] Error fetching data", ex);
            }
            finally
            {
                _opdSemaphore.Release();
                _isOPDFetching = false;
            }
        }

        /// <summary>
        /// ประมวลผลข้อมูลใน OPD Queue
        /// </summary>
        private async Task ProcessOPDQueueAsync()
        {
            // ป้องกันการทำงานซ้ำซ้อน
            if (_isOPDProcessing)
            {
                return;
            }

            // ตรวจสอบว่า Queue ว่างหรือไม่
            if (_opdQueue.IsEmpty)
            {
                return;
            }

            _isOPDProcessing = true;

            try
            {
                // ประมวลผลครั้งละ 1 รายการ
                if (_opdQueue.TryDequeue(out var data))
                {
                    _logger.LogInfo($"[OPD Process] Processing ID: {data.DrugDispenseopdId} (Remaining: {_opdQueue.Count})");

                    // แปลงกลับเป็น DrugDispenseipd เพื่อใช้กับ ProcessSingleOrder
                    var ipdData = new DrugDispenseipd
                    {
                        DrugDispenseipdId = data.DrugDispenseopdId,
                        PrescId = data.PrescId,
                        DrugRequestMsgType = data.DrugRequestMsgType,
                        Hl7Data = data.Hl7Data,
                        DrugDispenseDatetime = data.DrugDispenseDatetime,
                        RecieveStatus = data.RecieveStatus,
                        RecieveStatusDatetime = data.RecieveStatusDatetime,
                        RecieveOrderType = data.RecieveOrderType
                    };

                    await Task.Run(() =>
                    {
                        _processor.ProcessSingleOrder(
                            ipdData,
                            "OPD",
                            result =>
                            {
                                if (result.Success)
                                {
                                    _logger.LogInfo($"[OPD Process] ✓ Success ID: {data.DrugDispenseopdId}");
                                }
                                else
                                {
                                    _logger.LogWarning($"[OPD Process] ✗ Failed ID: {data.DrugDispenseopdId} - {result.Message}");
                                }
                            }
                        );
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[OPD Process] Error processing queue item", ex);
            }
            finally
            {
                _isOPDProcessing = false;
            }
        }

        /// <summary>
        /// ตรวจสอบว่า ID มีอยู่ใน OPD Queue แล้วหรือไม่
        /// </summary>
        private bool IsInOPDQueue(int id)
        {
            foreach (var item in _opdQueue)
            {
                if (item.DrugDispenseopdId == id)
                    return true;
            }
            return false;
        }

        #endregion

        #region Dispose Pattern

        /// <summary>
        /// ปิดและทำความสะอาด resources
        /// </summary>
        public void Dispose()
        {
            StopIPDService();
            StopOPDService();

            _ipdSemaphore?.Dispose();
            _opdSemaphore?.Dispose();

            ClearAllQueues();

            _logger.LogInfo("QueueBasedProcessor disposed");
        }

        #endregion
    }
}