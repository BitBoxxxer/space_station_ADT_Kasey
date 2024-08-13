using System.Linq;
using Content.Shared.Corvax.TTS;
using Content.Client.UserInterface.Controls;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Speech;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.SpeechBarks;

namespace Content.Client.VoiceMask;

[GenerateTypedNameReferences]
public sealed partial class VoiceMaskNameChangeWindow : FancyWindow
{
    public Action<string>? OnNameChange;
    public Action<string?>? OnVerbChange;
    public Action<string>? OnVoiceChange; // Corvax-TTS
    public Action<string>? OnBarkChange; // Corvax-TTS
    public Action<string>? OnPitchChange; // Corvax-TTS

    private List<(string, string)> _verbs = new();
    private List<TTSVoicePrototype> _voices = new(); // Corvax-TTS
    private List<BarkPrototype> _barks = new(); // ADT Barks

    private string? _verb;

    public VoiceMaskNameChangeWindow()
    {
        RobustXamlLoader.Load(this);

        NameSelectorSet.OnPressed += _ =>
        {
            OnNameChange?.Invoke(NameSelector.Text);
        };

        SpeechVerbSelector.OnItemSelected += args =>
        {
            OnVerbChange?.Invoke((string?) args.Button.GetItemMetadata(args.Id));
            SpeechVerbSelector.SelectId(args.Id);
        };

        // Corvax-TTS-Start
        if (IoCManager.Resolve<IConfigurationManager>().GetCVar(CCCVars.TTSEnabled))
        {
            TTSContainer.Visible = true;
            ReloadVoices(IoCManager.Resolve<IPrototypeManager>());
        }
        // Corvax-TTS-End
        // ADT Barks start
        if (IoCManager.Resolve<IConfigurationManager>().GetCVar(ADTCCVars.BarksEnabled))
        {
            BarksContainer.Visible = true;
            ReloadBarks(IoCManager.Resolve<IPrototypeManager>());
        }
        // ADT Barks end

        AddVerbs();
    }

    public void ReloadVerbs(IPrototypeManager proto)
    {
        foreach (var verb in proto.EnumeratePrototypes<SpeechVerbPrototype>())
        {
            _verbs.Add((Loc.GetString(verb.Name), verb.ID));
        }
        _verbs.Sort((a, b) => a.Item1.CompareTo(b.Item1));
    }

    private void AddVerbs()
    {
        SpeechVerbSelector.Clear();

        AddVerb(Loc.GetString("chat-speech-verb-name-none"), null);
        foreach (var (name, id) in _verbs)
        {
            AddVerb(name, id);
        }
    }

    private void AddVerb(string name, string? verb)
    {
        var id = SpeechVerbSelector.ItemCount;
        SpeechVerbSelector.AddItem(name);
        if (verb is {} metadata)
            SpeechVerbSelector.SetItemMetadata(id, metadata);

        if (verb == _verb)
            SpeechVerbSelector.SelectId(id);
    }

    // Corvax-TTS-Start
    private void ReloadVoices(IPrototypeManager proto)
    {
        VoiceSelector.OnItemSelected += args =>
        {
            VoiceSelector.SelectId(args.Id);
            if (VoiceSelector.SelectedMetadata != null)
                OnVoiceChange!((string)VoiceSelector.SelectedMetadata);
        };
        _voices = proto
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => o.RoundStart)
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();
        for (var i = 0; i < _voices.Count; i++)
        {
            var name = Loc.GetString(_voices[i].Name);
            VoiceSelector.AddItem(name);
            VoiceSelector.SetItemMetadata(i, _voices[i].ID);
        }
    }
    // Corvax-TTS-End

    // ADT Barks start
    private void ReloadBarks(IPrototypeManager proto)
    {
        BarkSelector.OnItemSelected += args =>
        {
            BarkSelector.SelectId(args.Id);
            if (BarkSelector.SelectedMetadata != null)
                OnBarkChange!((string)BarkSelector.SelectedMetadata);
        };
        PitchSelector.OnTextEntered += args =>
        {
            if (float.TryParse(args.Text, out var newMsg))
            {
                OnPitchChange!(args.Text);
                PitchSelector.SetText(args.Text);
            }
        };

        _barks = proto
            .EnumeratePrototypes<BarkPrototype>()
            .Where(o => o.RoundStart)
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();
        for (var i = 0; i < _barks.Count; i++)
        {
            var name = Loc.GetString(_barks[i].Name);
            BarkSelector.AddItem(name);
            BarkSelector.SetItemMetadata(i, _barks[i].ID);
        }
    }
    // ADT Barks end

    public void UpdateState(string name, string voice, string barkId, float barkPitch, string? verb) // Corvax-TTS
    {
        NameSelector.Text = name;
        _verb = verb;

        for (int id = 0; id < SpeechVerbSelector.ItemCount; id++)
        {
            if (string.Equals(verb, SpeechVerbSelector.GetItemMetadata(id)))
            {
                SpeechVerbSelector.SelectId(id);
                break;
            }
        }

        // Corvax-TTS-Start
        var voiceIdx = _voices.FindIndex(v => v.ID == voice);
        if (voiceIdx != -1)
            VoiceSelector.Select(voiceIdx);
        // Corvax-TTS-End

        // ADT Barks start
        var barkIdx = _barks.FindIndex(b => b.ID == barkId);
        if (barkIdx != -1)
            BarkSelector.Select(barkIdx);
        PitchSelector.SetText(barkPitch.ToString());
        // ADT Barks end
    }
}
