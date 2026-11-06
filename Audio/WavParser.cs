using System;
using System.IO;
using System.Text;
using UnityEngine;
using Logger = Silk.Logger;

namespace Jukebox
{
    public static class WavParser
    {
        private static readonly Guid PcmSubFormatGuid = new Guid("00000001-0000-0010-8000-00AA00389B71");
        private static readonly Guid IeeeFloatSubFormatGuid = new Guid("00000003-0000-0010-8000-00AA00389B71");

        public static AudioClip ParseFile(string path, string clipName)
        {
            byte[] wavData;
            try
            {
                wavData = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Jukebox] Could not read WAV file '{path}': {ex.Message}");
                return null;
            }

            return ParseWavData(wavData, clipName);
        }

        private static AudioClip ParseWavData(byte[] wavData, string clipName)
        {
            if (wavData == null || wavData.Length < 44)
            {
                Logger.LogWarning($"[Jukebox] WAV file too small: {clipName}");
                return null;
            }

            int pos = 12;

            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            bool decodeAsFloat = false;
            bool formatValidated = false;
            int bytesPerSample = 0;
            float[] samples = null;

            while (pos < wavData.Length - 8)
            {
                string chunkId = Encoding.ASCII.GetString(wavData, pos, 4);
                int chunkSize = BitConverter.ToInt32(wavData, pos + 4);

                if (chunkSize < 0 || chunkSize > wavData.Length - (pos + 8))
                {
                    Logger.LogWarning($"[Jukebox] Invalid chunk size {chunkSize} in file: {clipName}");
                    return null;
                }

                if (chunkId == "fmt ")
                {
                    ushort audioFormat = BitConverter.ToUInt16(wavData, pos + 8);
                    channels = BitConverter.ToInt16(wavData, pos + 10);
                    sampleRate = BitConverter.ToInt32(wavData, pos + 12);
                    bitsPerSample = BitConverter.ToInt16(wavData, pos + 22);
                    bytesPerSample = bitsPerSample / 8;

                    if (bitsPerSample <= 0 || bitsPerSample % 8 != 0)
                    {
                        Logger.LogWarning($"[Jukebox] Invalid bit depth: {bitsPerSample}. File: {clipName}");
                        return null;
                    }

                    if (audioFormat == 1)
                    {
                        formatValidated = true;
                        decodeAsFloat = false;
                    }
                    else if (audioFormat == 3)
                    {
                        formatValidated = true;
                        decodeAsFloat = true;
                    }
                    else if (audioFormat == 65534)
                    {
                        if (chunkSize < 40)
                        {
                            Logger.LogWarning($"[Jukebox] Invalid extensible fmt chunk size {chunkSize}. File: {clipName}");
                            return null;
                        }

                        byte[] subFormatBytes = new byte[16];
                        Buffer.BlockCopy(wavData, pos + 32, subFormatBytes, 0, 16);
                        Guid subFormatGuid = new Guid(subFormatBytes);

                        if (subFormatGuid == PcmSubFormatGuid)
                        {
                            formatValidated = true;
                            decodeAsFloat = false;
                        }
                        else if (subFormatGuid == IeeeFloatSubFormatGuid)
                        {
                            formatValidated = true;
                            decodeAsFloat = true;
                        }
                        else
                        {
                            Logger.LogWarning($"[Jukebox] Unsupported WAV extensible SubFormat GUID {subFormatGuid}. File: {clipName}");
                            return null;
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"[Jukebox] Unsupported WAV format {audioFormat}. File: {clipName}");
                        return null;
                    }
                }
                else if (chunkId == "data")
                {
                    if (!formatValidated)
                    {
                        Logger.LogWarning($"[Jukebox] WAV data chunk before valid fmt chunk: {clipName}");
                        return null;
                    }

                    int dataStart = pos + 8;
                    int dataSize = Math.Min(chunkSize, wavData.Length - dataStart);

                    if (!decodeAsFloat && bitsPerSample == 16)
                    {
                        int sampleCount = dataSize / 2;
                        samples = new float[sampleCount];
                        for (int i = 0; i < sampleCount; i++)
                        {
                            short sample = BitConverter.ToInt16(wavData, dataStart + i * 2);
                            samples[i] = sample / 32768f;
                        }
                    }
                    else if (!decodeAsFloat && bitsPerSample == 8)
                    {
                        int sampleCount = dataSize;
                        samples = new float[sampleCount];
                        for (int i = 0; i < sampleCount; i++)
                        {
                            samples[i] = (wavData[dataStart + i] - 128) / 128f;
                        }
                    }
                    else if (!decodeAsFloat && bitsPerSample == 24)
                        {
                        int sampleCount = dataSize / 3;
                        samples = new float[sampleCount];
                        for (int i = 0; i < sampleCount; i++)
                        {
                            int offset = dataStart + i * 3;
                            int sample = wavData[offset] | (wavData[offset + 1] << 8) | (wavData[offset + 2] << 16);
                            if ((sample & 0x800000) != 0)
                                sample |= unchecked((int)0xFF000000);
                            samples[i] = sample / 8388608f;
                        }
                    }
                    else if (decodeAsFloat && bitsPerSample == 32)
                    {
                        int sampleCount = dataSize / bytesPerSample;
                        samples = new float[sampleCount];
                        for (int i = 0; i < sampleCount; i++)
                        {
                            samples[i] = BitConverter.ToSingle(wavData, dataStart + i * bytesPerSample);
                        }
                    }
                    else
                    {
                        string encoding = decodeAsFloat ? "float" : "integer PCM";
                        Logger.LogWarning($"[Jukebox] Unsupported {encoding} bit depth: {bitsPerSample}. File: {clipName}");
                        return null;
                    }
                }

                pos += chunkSize + 8;
                if (chunkSize % 2 != 0)
                    pos++;
            }

            if (samples == null || channels == 0 || sampleRate == 0)
            {
                Logger.LogWarning($"[Jukebox] Invalid WAV file structure: {clipName}");
                return null;
            }

            int sampleLength = samples.Length / channels;
            AudioClip clip = AudioClip.Create(clipName, sampleLength, channels, sampleRate, false);
            clip.SetData(samples, 0);

            return clip;
        }
    }
}
