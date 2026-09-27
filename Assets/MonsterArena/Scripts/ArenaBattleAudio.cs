using UnityEngine;

// Local presentation only. Both clients hear events already observed by Cast/HUD.
public sealed class ArenaBattleAudio : MonoBehaviour
{
    private readonly AudioClip[] charges = new AudioClip[3];
    private readonly AudioClip[] launches = new AudioClip[3];
    private readonly AudioClip[] impacts = new AudioClip[3];
    private readonly AudioSource[] casts = new AudioSource[2];
    private readonly AudioSource[] hits = new AudioSource[2];
    private AudioSource resultSource;
    private AudioClip victory;
    private AudioClip defeat;
    private bool resultPlayed;
    public bool Diagnostics { get; set; }

    private void Awake()
    {
        Diagnostics = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-battleAudioDiagnostics") >= 0;
        for (int skill = 0; skill < 3; skill++)
        {
            string key = new[] { "Q", "W", "E" }[skill];
            charges[skill] = Load(key + "_Charge");
            launches[skill] = Load(key + "_Launch");
            impacts[skill] = Load(key + "_Impact");
        }
        for (int team = 0; team < 2; team++)
        {
            casts[team] = CreateSource("Cast " + team, .65f);
            hits[team] = CreateSource("Impact " + team, .65f);
        }
        victory = Load("Victory");
        defeat = Load("Defeat");
        resultSource = CreateSource("Result", .8f);
    }

    private static AudioClip Load(string name)
    {
        var clip = Resources.Load<AudioClip>("BattleAudio/ShadowFox/" + name);
        if (clip == null) Debug.LogWarning("[Battle Audio] Missing clip: " + name);
        return clip;
    }

    private AudioSource CreateSource(string label, float volume)
    {
        var child = new GameObject("Battle Audio " + label);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
        source.volume = volume;
        // AudioListener.volume is controlled by the existing settings slider.
        return source;
    }

    private static bool Valid(int team, int skill) => team >= 0 && team < 2 && skill >= 0 && skill < 3;

    public void BeginCharge(int team, int skill, float remainingWindup)
    {
        if (!isActiveAndEnabled || !Valid(team, skill)) return;
        var source = casts[team];
        source.Stop();
        var clip = charges[skill];
        if (clip == null || remainingWindup <= .01f) return;
        source.clip = clip;
        // A late network event starts at the matching point in the charge.
        source.time = Mathf.Clamp(clip.length - remainingWindup, 0, clip.length - .01f);
        source.Play();
        Trace("charge", team, clip.name);
    }

    public void Launch(int team, int skill)
    {
        if (!isActiveAndEnabled || !Valid(team, skill)) return;
        var source = casts[team];
        source.Stop();
        source.clip = launches[skill];
        if (source.clip != null)
        {
            source.Play();
            Trace("launch", team, source.clip.name);
        }
    }

    public void Impact(int team, int skill)
    {
        if (!isActiveAndEnabled || !Valid(team, skill) || impacts[skill] == null) return;
        hits[team].PlayOneShot(impacts[skill]);
        Trace("impact", team, impacts[skill].name);
    }

    public void PlayResult(int localHealth, int opponentHealth)
    {
        if (!isActiveAndEnabled || resultPlayed || (localHealth > 0 && opponentHealth > 0)) return;
        resultPlayed = true;
        // A draw must not announce either victory or defeat.
        if (localHealth <= 0 && opponentHealth <= 0) return;
        resultSource.clip = opponentHealth <= 0 ? victory : defeat;
        if (resultSource.clip != null)
        {
            resultSource.Play();
            Trace("result", -1, resultSource.clip.name);
        }
    }

    public void StopAll()
    {
        foreach (var source in casts) if (source != null) source.Stop();
        foreach (var source in hits) if (source != null) source.Stop();
        if (resultSource != null) resultSource.Stop();
    }

    public void ResetRound()
    {
        StopAll();
        resultPlayed = false;
        Trace("reset", -1, "round");
    }

    private void Trace(string phase, int team, string clip)
    {
        if (Diagnostics) Debug.Log("[Battle Audio] " + phase + " team=" + team + " clip=" + clip
            + " time=" + Time.realtimeSinceStartup.ToString("F3") + " volume=" + AudioListener.volume.ToString("F2"));
    }

    private void OnDisable() => StopAll();
}
