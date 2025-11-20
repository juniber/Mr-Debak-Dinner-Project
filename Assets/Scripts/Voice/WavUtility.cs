using UnityEngine;
using System;
using System.IO;
using System.Text;

public static class WavUtility
{
    // AudioClip을 WAV 포맷의 byte 배열로 변환
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream);

            var sampleCount = clip.samples;
            var frequency = clip.frequency;
            var channels = clip.channels;

            // WAV 헤더 작성
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2); // 파일 크기 - 8
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size (PCM은 16)
            writer.Write((ushort)1); // AudioFormat (1은 PCM)
            writer.Write((ushort)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2); // ByteRate
            writer.Write((ushort)(channels * 2)); // BlockAlign
            writer.Write((ushort)16); // BitsPerSample

            // 데이터 작성
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(sampleCount * 2);

            var samples = new float[sampleCount * channels];
            clip.GetData(samples, 0);

            // float(-1.0 ~ 1.0) 데이터를 short(-32768 ~ 32767)로 변환하여 기록
            foreach (var sample in samples)
            {
                var intSample = (short)(sample * short.MaxValue);
                writer.Write(intSample);
            }

            return stream.ToArray();
        }
    }
}
