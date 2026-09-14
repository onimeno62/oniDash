import { useEffect, useRef } from 'react';
import { usePlayer } from '../hooks/usePlayer';

type Mode = 'spectrum' | 'bars' | 'oscilloscope';

export function MusicVisualizer({ mode = 'spectrum', height = 42 }: { mode?: Mode; height?: number }) {
  const player = usePlayer();
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const sourceRef = useRef<MediaElementAudioSourceNode | null>(null);
  const contextRef = useRef<AudioContext | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    let frame = 0;
    try {
      const AudioContextCtor = window.AudioContext || (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!AudioContextCtor) return;
      const context = contextRef.current ?? new AudioContextCtor();
      contextRef.current = context;
      if (!sourceRef.current) {
        sourceRef.current = context.createMediaElementSource(player.audioElement);
        analyserRef.current = context.createAnalyser();
        analyserRef.current.fftSize = 256;
        sourceRef.current.connect(analyserRef.current);
        analyserRef.current.connect(context.destination);
      }
      const analyser = analyserRef.current;
      if (!analyser) return;
      const data = new Uint8Array(analyser.frequencyBinCount);
      const draw = () => {
        const ctx = canvas.getContext('2d');
        if (!ctx) return;
        const width = canvas.clientWidth * window.devicePixelRatio;
        const h = canvas.clientHeight * window.devicePixelRatio;
        if (canvas.width !== width || canvas.height !== h) { canvas.width = width; canvas.height = h; }
        ctx.clearRect(0, 0, width, h);
        analyser.getByteFrequencyData(data);
        ctx.globalAlpha = player.playing ? 0.85 : 0.22;
        if (mode === 'oscilloscope') {
          analyser.getByteTimeDomainData(data);
          ctx.beginPath();
          for (let i = 0; i < data.length; i += 1) { const x = (i / (data.length - 1)) * width; const y = (data[i] / 255) * h; if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y); }
          ctx.strokeStyle = 'rgba(255,255,255,.75)'; ctx.lineWidth = 1.5 * window.devicePixelRatio; ctx.stroke();
        } else {
          const bars = mode === 'bars' ? 28 : 64;
          const step = Math.max(1, Math.floor(data.length / bars));
          const gap = 2 * window.devicePixelRatio;
          const barWidth = Math.max(1, width / bars - gap);
          for (let i = 0; i < bars; i += 1) { let sum = 0; for (let j = 0; j < step; j += 1) sum += data[i * step + j] ?? 0; const value = sum / step / 255; const bh = Math.max(1, value * h); const x = i * (width / bars); ctx.fillStyle = 'rgba(255,255,255,.72)'; ctx.fillRect(x, h - bh, barWidth, bh); }
        }
        frame = requestAnimationFrame(draw);
      };
      frame = requestAnimationFrame(draw);
      const resume = () => { void context.resume(); };
      player.audioElement.addEventListener('play', resume);
      return () => { cancelAnimationFrame(frame); player.audioElement.removeEventListener('play', resume); };
    } catch { return () => cancelAnimationFrame(frame); }
  }, [mode, player.audioElement, player.playing]);

  return <canvas ref={canvasRef} aria-label={`${mode} visualizer`} className="pointer-events-none w-full" style={{ height }} />;
}
