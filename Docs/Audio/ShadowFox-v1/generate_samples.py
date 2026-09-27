"""Original procedural Shadow Fox audition sounds; Python + NumPy only."""
from pathlib import Path
import json
import math
import wave
import numpy as np

ROOT = Path(__file__).resolve().parent
SR = 48000
RNG = np.random.default_rng(2092026)
TAU = 2 * np.pi


def timeline(seconds):
    return np.arange(round(seconds * SR)) / SR


def edges(x, attack=.006, release=.045):
    x = x.copy()
    a, r = min(len(x), round(attack * SR)), min(len(x), round(release * SR))
    x[:a] *= np.sin(np.linspace(0, np.pi / 2, a)) ** 2
    x[-r:] *= np.sin(np.linspace(np.pi / 2, 0, r)) ** 2
    return x


def noise(seconds, low, high):
    n = round(seconds * SR)
    frequencies = np.fft.rfftfreq(n, 1 / SR)
    spectrum = np.fft.rfft(RNG.standard_normal(n))
    response = np.exp(-(frequencies / high) ** 4)
    if low:
        response *= 1 - np.exp(-(frequencies / low) ** 4)
    x = np.fft.irfft(spectrum * response, n)
    return x / max(np.std(x), 1e-9)


def sweep(t, start, end, rate=5):
    frequency = end + (start - end) * np.exp(-rate * t)
    return np.sin(TAU * np.cumsum(frequency) / SR)


def echo(x, wet=.18):
    out = np.pad(x, (0, round(.48 * SR)))
    for delay, gain in [(.061, 1), (.113, .7), (.179, .5), (.263, .32), (.397, .17)]:
        shift = round(delay * SR)
        out[shift:shift + len(x)] += x * gain * wet
    return edges(out, .003, .08)


def mix(events):
    out = np.zeros(max(round(start * SR) + len(x) for start, x in events))
    for start, x in events:
        i = round(start * SR)
        out[i:i + len(x)] += x
    return out


def charge(seconds, strength):
    t = timeline(seconds)
    u = t / seconds
    frequency = 115 + 145 * u ** 1.6
    phase = TAU * np.cumsum(frequency) / SR
    body = np.sin(phase + 1.3 * np.sin(phase * .501))
    shimmer = np.sin(phase * 3.005) * .13 + np.sin(phase * 4.99) * .055
    wind = noise(seconds, 170, 2000) * .16
    pulse = .76 + .24 * np.sin(TAU * (3 * t + 2.5 * t * t))
    return edges((body * .28 + shimmer + wind) * pulse * (.1 + .9 * u ** .8) * strength,
                 .04, .035)


def launch(skill):
    length = [.63, .44, .94][skill]
    t = timeline(length)
    airy = noise(length, [350, 850, 180][skill], [3400, 6500, 2700][skill])
    if skill == 0:
        x = .56 * sweep(t, 510, 95, 9) * np.exp(-6 * t)
        x += .19 * airy * np.exp(-7 * t)
        x += .10 * sweep(t, 1120, 380, 6) * np.exp(-10 * t)
    elif skill == 1:
        envelope = (1 - np.exp(-100 * t)) * np.exp(-16 * t)
        x = airy * .38 * envelope + .28 * sweep(t, 1300, 175, 17) * envelope
        x += .07 * np.sin(TAU * 2100 * t) * np.exp(-23 * t)
    else:
        x = .43 * sweep(t, 260, 58, 6) * np.exp(-3.8 * t)
        x += .24 * airy * (.7 + .3 * np.cos(TAU * 13 * t)) * np.exp(-4.8 * t)
        x += .09 * sweep(t, 710, 150, 4) * np.exp(-4 * t)
    return edges(x, .005, .075)


def hit(skill):
    length = [.65, .51, 1.12][skill]
    t = timeline(length)
    bass = sweep(t, [170, 220, 130][skill], [58, 85, 43][skill], 22)
    x = bass * [.48, .34, .62][skill] * np.exp(-[9, 15, 5][skill] * t)
    x += noise(length, 250, [4700, 6500, 3600][skill]) * .28 * np.exp(-[22, 29, 14][skill] * t)
    for frequency, amplitude in [(690, .07), (1120, .055), (1790, .025)]:
        x += np.sin(TAU * frequency * t) * amplitude * np.exp(-9 * t)
    if skill == 2:
        x += noise(length, 90, 1200) * .20 * np.exp(-4.5 * t)
    return echo(edges(x, .002, .08), .14)


def bell(midi, seconds, gain):
    t = timeline(seconds)
    f = 440 * 2 ** ((midi - 69) / 12)
    x = np.sin(TAU * f * t) * np.exp(-2.7 * t)
    x += .29 * np.sin(TAU * f * 2.002 * t) * np.exp(-4.8 * t)
    x += .09 * np.sin(TAU * f * 3.997 * t) * np.exp(-8 * t)
    return edges(x * gain, .009, .18)


def result(won):
    melody = [74, 77, 81, 86] if won else [74, 72, 69, 62]
    notes = [(i * .19, bell(note, 1.5, .26 if won else .21)) for i, note in enumerate(melody)]
    chord = [50, 57, 62, 65] if won else [46, 53, 58, 62]
    for note in chord:
        notes.append((.60, bell(note, 1.9, .11)))
    return echo(mix(notes), .24)


def master(x, peak_db):
    # Gentle saturation controls layered transients before setting safe peak level.
    x = np.tanh(x * 1.12)
    x -= np.mean(x)
    x = edges(x, .002, .04)
    return x * (10 ** (peak_db / 20) / max(np.max(np.abs(x)), 1e-9))


def write_wav(name, x):
    path = ROOT / name
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.round(x * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    with wave.open(str(path), 'rb') as f:
        assert f.getframerate() == SR and f.getnchannels() == 1 and f.getsampwidth() == 2
        decoded = np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(float) / 32768
    assert np.isfinite(decoded).all()
    assert np.max(np.abs(decoded)) < .90
    assert abs(decoded[0]) < .001 and abs(decoded[-1]) < .001
    return {'file': name, 'seconds': round(len(decoded) / SR, 3),
            'peak_dbfs': round(20 * math.log10(max(np.max(np.abs(decoded)), 1e-9)), 2),
            'rms_dbfs': round(20 * math.log10(max(np.sqrt(np.mean(decoded ** 2)), 1e-9)), 2),
            'clipped_samples': int(np.sum(np.abs(decoded) >= .999)),
            'max_sample_step': round(float(np.max(np.abs(np.diff(decoded)))), 4)}


def main():
    stats, audition = [], []
    offsets = [0.58 * .55, .79 * .55, 3.5 * .55]
    # Audition-only flight lengths. Integration must trigger impact from actual arrival.
    flights = [.52, .36, .75]
    for skill, key in enumerate('QWE'):
        windup = offsets[skill]
        cast = mix([(0, charge(windup, [.40, .22, .72][skill])),
                    (windup, launch(skill))])
        cast = master(echo(cast, .13), [-6, -7, -5][skill])
        impact = master(hit(skill), [-5, -6, -4][skill])
        stats.append(write_wav(f'Stems/{key}_Cast.wav', cast))
        stats.append(write_wav(f'Stems/{key}_Impact.wav', impact))
        demo = mix([(0, cast), (windup + flights[skill], impact)])
        if np.max(np.abs(demo)) > .80:
            demo *= .80 / np.max(np.abs(demo))
        stats.append(write_wav(f'{skill + 1:02d}_{key}_Preview.wav', demo))
        audition.append((key, demo))
    for index, won, name in [(4, True, 'Victory'), (5, False, 'Defeat')]:
        x = master(result(won), -8)
        stats.append(write_wav(f'{index:02d}_{name}.wav', x))
        audition.append((name, x))
    events, chapters, position = [], [], .4
    for label, x in audition:
        events.append((position, x))
        chapters.append({'sound': label, 'start_seconds': round(position, 2),
                         'end_seconds': round(position + len(x) / SR, 2)})
        position += len(x) / SR + .9
    all_sounds = np.pad(mix(events), (0, round(.4 * SR)))
    stats.append(write_wav('00_All_Previews.wav', all_sounds))
    report = {'format': 'PCM WAV, mono, 48000 Hz, 16-bit',
              'seed': 2092026, 'chapters': chapters, 'files': stats,
              'verification': 'Decoded WAV headers, samples, peak headroom, finite values and endpoint fades checked. No human listening or Unity runtime verification.'}
    (ROOT / 'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
