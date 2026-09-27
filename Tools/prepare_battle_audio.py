"""Prepare the approved WAVs for event-driven Unity playback; do not resynthesize."""
from pathlib import Path
import hashlib
import json
import shutil
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Docs/Audio/ShadowFox-v1'
TARGET = ROOT / 'Assets/MonsterArena/Resources/BattleAudio/ShadowFox'
REPORT = ROOT / 'Docs/Validation/BattleAudio'
TARGET.mkdir(parents=True, exist_ok=True)
REPORT.mkdir(parents=True, exist_ok=True)
files = []


def record(path, original):
    with wave.open(str(path), 'rb') as f:
        assert (f.getnchannels(), f.getsampwidth(), f.getframerate()) == (1, 2, 48000)
        data = np.frombuffer(f.readframes(f.getnframes()), '<i2')
    assert len(data) > 0 and np.max(np.abs(data.astype(float))) < 32767
    files.append({'file': str(path.relative_to(ROOT)), 'source': str(original.relative_to(ROOT)),
                  'samples': len(data), 'seconds': len(data) / 48000,
                  'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})


for key, windup in zip('QWE', [.319, .4345, 1.925]):
    original = SOURCE / 'Stems' / f'{key}_Cast.wav'
    with wave.open(str(original), 'rb') as f:
        params = f.getparams()
        pcm = np.frombuffer(f.readframes(f.getnframes()), '<i2').copy()
    cut = round(windup * params.framerate)
    for name, data in [('Charge', pcm[:cut].copy()), ('Launch', pcm[cut:].copy())]:
        # The split crosses a nonzero waveform. A 4 ms taper avoids boundary clicks.
        fade = round(.004 * params.framerate)
        if name == 'Charge':
            data[-fade:] = np.round(data[-fade:] * np.linspace(1, 0, fade)).astype('<i2')
        else:
            data[:fade] = np.round(data[:fade] * np.linspace(0, 1, fade)).astype('<i2')
        path = TARGET / f'{key}_{name}.wav'
        with wave.open(str(path), 'wb') as f:
            f.setparams(params)
            f.writeframes(data.astype('<i2').tobytes())
        record(path, original)
    original = SOURCE / 'Stems' / f'{key}_Impact.wav'
    path = TARGET / original.name
    shutil.copyfile(original, path)
    record(path, original)

for name, source_name in [('Victory', '04_Victory.wav'), ('Defeat', '05_Defeat.wav')]:
    original = SOURCE / source_name
    path = TARGET / f'{name}.wav'
    shutil.copyfile(original, path)
    record(path, original)

(REPORT / 'Assets.json').write_text(json.dumps(files, indent=2), encoding='utf-8')
print(f'Prepared and verified {len(files)} WAV assets. Approved impact/result bytes preserved.')
