using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using auxmic.fft;
using NAudio.Wave;
using System.Linq;

namespace auxmic
{
    public sealed class Clip : INotifyPropertyChanged
    {
        #region PROPERTIES & FIELDS
        /// <summary>
        /// Параметры синхронизации клипа
        /// </summary>
        internal SyncParams _syncParams;

        /// <summary>
        /// Формат мастер-записи к которому надо ресемплировать остальные файлы для дальнейшей работы с ними.
        /// </summary>
        internal WaveFormat _resampleFormat;

        internal CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Звуковой файл (с заголовком и методом чтения данных)
        /// </summary>
        internal SoundFile SoundFile { get; set; }

        private string _filename;
        /// <summary>
        /// Полное имя файла
        /// </summary>
        public string Filename
        {
            get { return _filename; }
            set
            {
                _filename = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Filename"));
            }
        }

        private string _displayname;
        /// <summary>
        /// Отображаемое имя файла (Filename без пути)
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_displayname == null)
                {
                    _displayname = Path.GetFileName(_filename);
                }

                return _displayname;
            }

            set
            {
                _displayname = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayName"));
            }
        }

        private int _maxProgressValue;
        /// <summary>
        /// Максимальное значения прогресса.
        /// ВНИМАНИЕ: на разных этапах обработки файла это значение меняется.
        /// Сначала отображает максимальное значение прогресса при расчёте хэшей,
        /// затем отображает значение для синхронизации.
        /// </summary>
        public int MaxProgressValue
        {
            get { return _maxProgressValue; }
            set
            {
                _maxProgressValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("MaxProgressValue"));
            }
        }

        private int _progressValue;
        /// <summary>
        /// Текущее значение прогресса.
        /// ВНИМАНИЕ: отображает сначала процесс расчёта хэшей, затем матчинга (синхронизации)
        /// </summary>
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ProgressValue"));
            }
        }

        private bool _isLoading;
        /// <summary>
        /// WAV извлекается из медиа-файла и сохраняется во временной директории.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsLoading"));
            }
        }

        private bool _isMatching;
        /// <summary>
        /// Синхронизация в процессе выполнения?
        /// Это свойство потребовалось для отображения прогресса выполнения синхронизации 
        /// другим цветом на одном и том же элементе ProgressBar. Изначально для выбора
        /// другого цвета отслеживалось свойство IsHashed, но тогда после хэширования 
        /// прогресс показывал 100% и менял цвет, в то время как требовалось менять цвет
        /// только с началом синхронизации.
        /// </summary>
        public bool IsMatching
        {
            get { return _isMatching; }
            set
            {
                _isMatching = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMatching"));
            }
        }

        private bool _isMatched;
        /// <summary>
        /// Файл уже синхронизирован?
        /// </summary>
        public bool IsMatched
        {
            get { return _isMatched; }
            set
            {
                _isMatched = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMatched"));
            }
        }

        private bool _isHashed;
        /// <summary>
        /// Хэши для файла посчитаны?
        /// </summary>
        public bool IsHashed
        {
            get { return _isHashed; }
            set
            {
                _isHashed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsHashed"));
            }
        }

        private Int32[] _hashes;
        /// <summary>
        /// Хэши для файла
        /// </summary>
        internal Int32[] Hashes
        {
            get
            {
                if (_hashes == null)
                {
                    CalcHashes();
                }

                return _hashes;
            }

            private set
            {
                _hashes = value;

                this.IsHashed = true;

                // raise event if not canceled
                if (!IsCanceled) OnHashed(null);
            }
        }

        /// <summary>
        /// Количество сэмплов данных
        /// </summary>
        internal int DataLength
        {
            get
            {
                return (this.SoundFile != null) ? this.SoundFile.DataLength : 0;
            }
        }

        /// <summary>
        /// Позиция с которой начинается LQ-файл в мастере.
        /// Значение можно вычислить зная this.Offset как
        /// clip.Offset.TotalSeconds * clip.WaveFormat.SampleRate,
        /// но вычисленное значение будет немного отличаться от изначально рассчитанного.
        /// Разница мала, и можно ею пренебречь, но решил хранить изначально рассчитанное значение.
        /// </summary>
        internal int StartIndex { get; set; }

        private TimeSpan _offset;

        /// <summary>
        /// Смещение файла относительно мастер-записи
        /// </summary>
        public TimeSpan Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                _offset = value;

                OnPropertyChanged(new PropertyChangedEventArgs("Offset"));

                this.IsMatched = true;

                // raise event if not canceled
                if (!IsCanceled) OnSynced(null);
            }
        }

        private bool IsCanceled { get; set; }

        /// <summary>
        /// Заголовок с форматом данных. Свойство позволяет не делать публичным SoundFile.
        /// </summary>
        public WaveFormat WaveFormat
        {
            get
            {
                return this.SoundFile.WaveFormat;
            }
        }
        #endregion

        #region EVENTS
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public void ReportProgress(int progress)
        {
            if (this.IsMatching)
            {
                //? this.ProgressValue = Interlocked.Increment(ref _progressValue);
                this.ProgressValue++;
            }
            else
            {
                this.ProgressValue = progress;
            }

            EventHandler<ProgressChangedEventArgs> handler = ProgressChanged;

            if (handler != null)
            {
                handler(this, new ProgressChangedEventArgs(progress, null));
            }
        }

        public event EventHandler Hashed;
        /// <summary>
        /// Событие окончания расчёта хэшей.
        /// </summary>
        /// <param name="e"></param>
        public void OnHashed(EventArgs e)
        {
            if (Hashed != null)
            {
                Hashed(this, e);
            }
        }

        public event EventHandler Synced;
        /// <summary>
        /// Событие окончания синхронизации.
        /// </summary>
        /// <param name="e"></param>
        public void OnSynced(EventArgs e)
        {
            if (Synced != null)
            {
                Synced(this, e);
            }
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Filename to load</param>
        /// <param name="syncParams">SyncParams</param>
        /// <param name="resampleFormat">Resample format. If not set (null) - will not resample.</param>
        internal Clip(string filename, SyncParams syncParams, WaveFormat resampleFormat = null)
        {
            this.Filename = filename;
            this._syncParams = syncParams;
            this._resampleFormat = resampleFormat;
            SetProgressMax();
        }

        /// <summary>
        /// Загрузка звукового файла. Файл извлекается из медиа-контейнера, ресемплируется
        /// и в виде WAV-фала сохрнаяется во временную директорию.
        /// Если формат файла не поддерживается Media Foundation выдаст исключение 
        /// COMException MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED,
        /// которое перехватывается и оборачивается в NotSupportedException.
        /// </summary>
        internal void LoadFile()
        {
            IsLoading = true;

            try
            {
                this.SoundFile = new SoundFile(this.Filename, this._resampleFormat);
            }
            catch (COMException ex)
            {
                this.CancellationTokenSource.Cancel();

                // HRESULT: 0xC00D36C4 (-1072875836) - 
                int MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED = -1072875836;

                if (ex.ErrorCode == MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED)
                {
                    throw new NotSupportedException(String.Format("'{0}' not supported.", this.DisplayName), ex);
                }
            }
            catch (Exception)
            {
                this.CancellationTokenSource.Cancel();

                throw;
            }
            finally
            {
                this.IsLoading = false;
            }

            if (this.CancellationTokenSource.IsCancellationRequested)
            {
                this.Dispose();
            }
        }

        internal void SaveMatch(string filename, int startIndex, int length)
        {
            this.SoundFile.SaveMatch(filename, startIndex, length);
        }

        /// <summary>
        /// Хэширование массива (кастомное)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int CombineHashCodes(params Int16[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Length == 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (data.Length == 1)
            {
                return data[0];
            }

            int result = data[0];

            for (var i = 1; i < data.Length; i++)
            {
                result = (result << 5) | (result >> 29) ^ data[i];
            }

            return result;
        }

        /// <summary>
        /// Расчёт хэшей от максимальных магнитуд по окнам для всего звукового файла.
        /// </summary>
        /// <returns></returns>
        private Int32[] GetHashes()
        {
            // check is cached value exists
            string cachedFilename = GetCachedFilename();

            if (FileCache.Contains(cachedFilename))
            {
                ReportProgress(this.MaxProgressValue);

                return FileCache.Get<Int32[]>(cachedFilename);
            }

            int L = _syncParams.L;
            int N = this.DataLength;

            Complex[] segment = new Complex[L];

            int ranges = (int)Math.Ceiling(((decimal)(L / 2) / _syncParams.FreqRangeStep));

            Int32[] hashes = new Int32[N / L];

            int row = 0;

            // перебираем все данные по т.н. окнам
            for (int i = 0; i <= N - L; i += L)
            {
                this.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                // читаем необходимое количество данных из левого канала в "окно"
                segment = this.SoundFile.ReadComplex(L);

                // применяем оконную функцию через делегат
                _syncParams.WindowFunction(segment);

                // In-place FFT-преобразование этого временного массива
                Fourier.FFT(segment, Direction.Backward);

                // считаем powers в этом же цикле
                // это массив частот с максимальными магнитудами
                // диапазон частот задаётся шагом rangeStep от 0
                Int16[] segmentPowers = new Int16[ranges];

                double[] maxMagnitude = new double[ranges];

                for (Int16 freq = 0; freq < L / 2; freq++)
                {
                    double magnitude = Complex.Abs(Complex.Log10(segment[freq]));

                    // определяем в какой диапазон частот с шагом rangeStep попадает текущая частота
                    int index = freq / _syncParams.FreqRangeStep;

                    // если найдена максимальная магнитуда, запоминаем её частоту в powers
                    if (magnitude > maxMagnitude[index])
                    {
                        maxMagnitude[index] = magnitude;
                        segmentPowers[index] = freq;
                    }
                }

                // расчитываем хэши по каждому окну на основе power - массива частот с максимальными магнитудами
                hashes[row] = CombineHashCodes(segmentPowers);

                row++;

                ReportProgress(row);
            }

            // add to cache
            FileCache.Set(cachedFilename, hashes);

            return hashes;
        }

        internal void SetProgressMax(Int32 masterHashLength = 0)
        {
            if (this.SoundFile == null)
            {
                // файл пока копируется
                this.MaxProgressValue = 0;
            }
            else if (masterHashLength == 0)
            {
                // максимальное значение прогресса для расчётов хэшей
                this.MaxProgressValue = this.DataLength / _syncParams.L;
            }
            else
            {
                // максимальное значение прогресса для синхронизации
                this.MaxProgressValue = masterHashLength - this.Hashes.Length + 1;
            }
        }

        /// <summary>
        /// Метод запуска расчёта хэшей.
        /// </summary>
        internal void CalcHashes()
        {
            SetProgressMax();

            this.Hashes = GetHashes();
        }

        /// <summary>
        /// Метод запуска синхронизации файла.
        /// </summary>
        /// <param name="master"></param>
        internal void Sync(Clip master)
        {
            SetProgressMax(master.Hashes.Length);

            this.StartIndex = Match(master);

            this.Offset = TimeSpan.FromSeconds((double)this.StartIndex / this.WaveFormat.SampleRate);
        }

        /// <summary>
        /// Инициация отмены обработки файла с дальнейшим освобождением ресурсов и очисткой кэша.
        /// </summary>
        internal void Cancel()
        {
            // инициируем запрос на отмену задачи
            this.CancellationTokenSource.Cancel();

            // выставлем свойство для отображения в GUI
            this.IsCanceled = true;

            // останавливаем обработку (расчёт хэшей и синхронизацию),
            // удаляем закэшированные данные, если есть,
            // удаляем временную wav-копию
            this.Dispose();
        }

        /// <summary>
        /// Метод остановки всех обработок. Освобождает ресурсы. Удаляет кэш. Удаляет временную копию.
        /// </summary>
        internal void Dispose()
        {
            // освобождаем ридер
            if (this.SoundFile != null) this.SoundFile.Dispose();

            // удаляем закэшированные данные, если есть
            FileCache.Remove(GetCachedFilename());

            // удаляем временную wav-копию
            RemoveTempFile();
        }

        /// <summary>
        /// Удаление временной копии wav-файла.
        /// </summary>
        private void RemoveTempFile()
        {
            if (this.SoundFile != null && File.Exists(this.SoundFile.TempFilename))
            {
                File.Delete(this.SoundFile.TempFilename);
            }
        }

        /// <summary>
        /// Матчинг (синхронизация) файла.
        /// </summary>
        /// <param name="master"></param>
        /// <returns></returns>
        private Int32 Match(Clip master)
        {
            if (master == null)
            {
                throw new ApplicationException("No master file specified.");
            }

            if (!master.IsHashed)
            {
                throw new ApplicationException(String.Format("Master file '{0}' has not processed yet.", master.Filename));
            }

            if (!this.IsHashed)
            {
                throw new ApplicationException(String.Format("File '{0}' has not processed yet.", this.Filename));
            }

            // для отображения прогресса другим цветом следим за этим свойством
            this.IsMatching = true;
            this.ProgressValue = 0;

            int startIndex = Match(master.Hashes, this.Hashes);

            return startIndex * this._syncParams.L;
        }

        private int Match(int[] hq_hashes, int[] lq_hashes)
        {
            int maxMatches = 0;

            int start = -lq_hashes.Length + 1;
            int end = hq_hashes.Length;

            int startIndex = start;

            //for (int hq_idx = start; hq_idx < end; hq_idx++)
            Parallel.For(start, end, hq_idx =>
            {
                this.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                int intersectionStart = Math.Max(hq_idx, 0);

                int intersectionLength = Min(
                    lq_hashes.Length,
                    lq_hashes.Length + hq_idx,
                    hq_hashes.Length - hq_idx,
                    hq_hashes.Length);

                int matchesCount = 0;

                for (int intersectionIndexHQ = intersectionStart; 
                     intersectionIndexHQ < intersectionLength + intersectionStart; 
                     intersectionIndexHQ++)
                {
                    int intersectionIndexLQ = intersectionIndexHQ - hq_idx;

                    if (hq_hashes[intersectionIndexHQ] == lq_hashes[intersectionIndexLQ])
                    {
                        matchesCount++;
                    }
                }

                if (matchesCount > maxMatches)
                {
                    maxMatches = matchesCount;
                    startIndex = hq_idx;
                }

                ReportProgress(hq_idx + 1);
            });

            return startIndex;
        }

        private int Min(params int[] args)
        {
            return args.Min();
        }

        /// <summary>
        /// Возвращает имя уже посчитанных хэшей для файла.
        /// Оказалось, что важно различать временные файлы с посчитанными хэшами по SampleRate,
        /// без различения по SampleRate возможна ситуация, когда загрузится кэш хэшей, посчитанный по 
        /// другому SampleRate. Такое возможно, если обработать два файла с различными SampleRate, 
        /// а затем поменять их местами (мастером сделать другой).
        /// Если у клипа не задан _resampleFormat, то это мастер-запись и берём SampleRate из SoundFile.
        /// </summary>
        /// <returns></returns>
        private string GetCachedFilename()
        {
            return FileCache.GetTempFilename(
                    this.Filename,
                    this._resampleFormat == null ? this.SoundFile.WaveFormat.SampleRate : this._resampleFormat.SampleRate);
        }
    }
}
