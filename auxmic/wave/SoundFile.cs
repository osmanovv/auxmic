using System;
using NAudio.Wave;
using System.Numerics;

namespace auxmic
{
    internal sealed class SoundFile : IDisposable
    {
        /// <summary>
        /// Имя исходного файла - может быть аудио- или видео-контейнер
        /// </summary>
        internal string Filename { get; set; }

        /// <summary>
        /// Имя извлечённого и ресемплированного файла WAV сохранённого во временной директории
        /// </summary>
        internal string TempFilename { get; set; }

        internal WaveFormat WaveFormat { get; set; }

        internal WaveFileReader FileReader { get; set; }

        internal int DataLength { get; set; }

        internal SoundFile(string filename, WaveFormat resampleFormat)
        {
            this.Filename = filename;
            this.TempFilename = FileCache.ComposeTempFilename(filename);

            // if such file already exists, do not create it again
            if (!FileCache.Exists(this.TempFilename))
            {
                ExtractAndResampleAudio(resampleFormat);
            }
            
            ReadWave(this.TempFilename);
        }

        private void ReadWave(string filename)
        {
            this.FileReader = new WaveFileReader(filename);

            this.WaveFormat = this.FileReader.WaveFormat;

            this.DataLength = (int)(this.FileReader.Length / this.WaveFormat.BlockAlign);
        }

        private void ExtractAndResampleAudio(WaveFormat resampleFormat)
        {
            using (var reader = new MediaFoundationReader(this.Filename))
            {
                if (NeedResample(reader.WaveFormat, resampleFormat))
                {
                    using (var resampler = new MediaFoundationResampler(reader, CreateOutputFormat(resampleFormat ?? reader.WaveFormat)))
                    {
                        WaveFileWriter.CreateWaveFile(this.TempFilename, resampler);
                    }
                }
                else
                {
                    WaveFileWriter.CreateWaveFile(this.TempFilename, reader);
                }
            }
        }

        private bool NeedResample(WaveFormat inputFormat, WaveFormat resampleFormat)
        {
            // даже если resampleFormat не задан, необходимо проверять
            // сколько каналов в файле и ресемплировать до 1 канала
            if (inputFormat.Channels > 2)
            {
                return true;
            }

            if (resampleFormat == null)
            {
                return false;
            }

            // TODO: test if BitsPerSample check needed
            return inputFormat.SampleRate != resampleFormat.SampleRate; /* || (inputFormat.BitsPerSample != resampleFormat.BitsPerSample)*/
        }

        private WaveFormat CreateOutputFormat(WaveFormat resapleFormat)
        {
            int channels = 1;

            WaveFormat waveFormat = new WaveFormat(resapleFormat.SampleRate, resapleFormat.BitsPerSample, channels);

            return waveFormat;
        }

        /// <summary>
        /// http://mark-dot-net.blogspot.ru/2009/09/trimming-wav-file-using-naudio.html
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        internal void SaveMatch(string filename, int startIndex, int length)
        {
            int startPos = startIndex * this.WaveFormat.BlockAlign;
            int endPos = (startIndex + length) * this.WaveFormat.BlockAlign;

            using (var reader = new WaveFileReader(this.TempFilename))
            using (var writer = new WaveFileWriter(filename, this.WaveFormat))
            {
                // if there is a negative offset from master record
                // start exporting from the beginning of the master
                // see issue #5:
                //   ArgumentOutOfRangeException while exporting synced file with negative offset
                reader.Position = startPos >= 0 ? startPos : 0;
                byte[] buffer = new byte[1024];

                while (reader.Position < Math.Min(endPos, reader.Length))
                {
                    int bytesRequired = (int)(endPos - reader.Position);
                    if (bytesRequired > 0)
                    {
                        int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                        int bytesRead = reader.Read(buffer, 0, bytesToRead);
                        if (bytesRead > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Читает данные _левого_ канала в массив Int32
        /// </summary>
        /// <param name="samplesToRead"></param>
        /// <returns></returns>
        internal Int32[] Read(int samplesToRead)
        {
            Int32[] result = new Int32[samplesToRead];

            int blockAlign = this.WaveFormat.BlockAlign;
            int channels = this.WaveFormat.Channels;

            byte[] buffer = new byte[blockAlign * samplesToRead];

            int bytesRead = this.FileReader.Read(buffer, 0, blockAlign * samplesToRead);

            for (int sample = 0; sample < bytesRead / blockAlign; sample++)
            {
                switch (this.WaveFormat.BitsPerSample)
                {
                    case 8:
                        result[sample] = (Int16)buffer[sample * blockAlign];
                        break;

                    case 16:
                        result[sample] = BitConverter.ToInt16(buffer, sample * blockAlign);
                        break;

                    case 32:
                        result[sample] = BitConverter.ToInt32(buffer, sample * blockAlign);
                        break;

                    default:
                        throw new NotSupportedException(String.Format("BitDepth '{0}' not supported. Try 8, 16 or 32-bit audio instead.", this.WaveFormat.BitsPerSample));
                }
            }

            return result;
        }

        /// <summary>
        /// Метод полностью повторяет <see cref="internal Int32[] Read(int samplesToRead)"/>, 
        /// за исключением возвращаемого значения, не Int32[], а сразу Complex[].
        /// Различаются только одной строкой: Complex[] result = new Complex[samplesToRead];
        /// Не получилось сделать через generics
        /// </summary>
        /// <param name="samplesToRead"></param>
        /// <returns></returns>
        internal Complex[] ReadComplex(int samplesToRead)
        {
            Complex[] result = new Complex[samplesToRead];

            int blockAlign = this.WaveFormat.BlockAlign;
            int channels = this.WaveFormat.Channels;

            byte[] buffer = new byte[blockAlign * samplesToRead];

            int bytesRead = this.FileReader.Read(buffer, 0, blockAlign * samplesToRead);

            for (int sample = 0; sample < bytesRead / blockAlign; sample++)
            {
                switch (this.WaveFormat.BitsPerSample)
                {
                    case 8:
                        result[sample] = (Int16)buffer[sample * blockAlign];
                        break;

                    case 16:
                        result[sample] = BitConverter.ToInt16(buffer, sample * blockAlign);
                        break;

                    case 32:
                        result[sample] = BitConverter.ToInt32(buffer, sample * blockAlign);
                        break;

                    default:
                        throw new NotSupportedException(String.Format("BitDepth '{0}' not supported. Try 8, 16 or 32-bit audio instead.", this.WaveFormat.BitsPerSample));
                }
            }

            return result;
        }

        //~SoundFile()
        //{
        //    Dispose();
        //}

        // TODO: Check releasing FileReader - test are not run well - file locked!
        public void Dispose()
        {
            if (this.FileReader != null)
            {
                this.FileReader.Close();
            }
        }
    }
}