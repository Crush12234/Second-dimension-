using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.M2;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Presentation.Boot
{
    // Explicit packaged-player QA only. All game changes go through visible,
    // raycastable shipping controls and normal battle Auto. Reflection reads
    // immutable state and isolation paths; it never writes campaign fields.
    public sealed class CampaignAlphaPlayerLoop132 : MonoBehaviour
    {
        const string Prefix = "--sd-campaign-loop132-";
        public sealed class Settings132 { public string Source, Output, Save; public int Through = 15; }
        public static Settings132 Prepared132 { get; private set; }
        public static bool IsRequested132(IReadOnlyList<string> args) => args != null && args.Any(a => a != null && a.StartsWith("--sd-campaign-loop132", StringComparison.OrdinalIgnoreCase));
        public static string Prepare132(IReadOnlyList<string> args, string personal, string data)
        {
            Prepared132 = null;
            var settings = new Settings132(); var seen = new HashSet<string>();
            foreach (var arg in args)
            {
                if (arg == null || !arg.StartsWith("--sd-", StringComparison.OrdinalIgnoreCase)) continue;
                var split = arg.IndexOf('=');
                if (!arg.StartsWith(Prefix, StringComparison.Ordinal) || split < 0) throw new InvalidOperationException("Campaign loop requires its own explicit isolation flags.");
                var key = arg.Substring(Prefix.Length, split-Prefix.Length); var value = arg.Substring(split+1);
                if (!seen.Add(key)) throw new InvalidOperationException("Duplicate Campaign loop flag.");
                if (key == "source") settings.Source = value;
                else if (key == "output") settings.Output = value;
                else if (key == "through-chapter" && int.TryParse(value, out var chapter)) settings.Through = chapter;
                else throw new InvalidOperationException("Invalid Campaign loop flag.");
            }
            if (settings.Through < 1 || settings.Through > 15) throw new InvalidOperationException("This focused Campaign run is bounded to chapters 1 through 15.");
            settings.Save = ManualEarnedSaveReview097.Prepare097(new[] {
                ManualEarnedSaveReview097.SourceFlag097+settings.Source,
                ManualEarnedSaveReview097.OutputFlag097+settings.Output
            },personal,data);
            Prepared132 = settings;
            return settings.Save;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create132()
        {
            if (Application.isEditor || !IsRequested132(Environment.GetCommandLineArgs())) return;
            var root = new GameObject("Opt-in Campaign Chapter15 Player Loop 132"); DontDestroyOnLoad(root);
            root.AddComponent<CampaignAlphaPlayerLoop132>();
        }

        static readonly BindingFlags Hidden = BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly FieldInfo CoordinatorField = typeof(M1FlowPresenter).GetField("_coordinator",Hidden);
        static readonly FieldInfo CampaignField = typeof(M1RuntimeCoordinator).GetField("_campaign",Hidden);
        static readonly FieldInfo PathField = typeof(M1RuntimeCoordinator).GetField("_savePath",Hidden);
        readonly Stopwatch _clock = new Stopwatch();
        readonly List<string> _errors = new List<string>();
        readonly HashSet<string> _chapterScreens = new HashSet<string>();
        readonly HashSet<string> _battles = new HashSet<string>();
        readonly HashSet<string> _threeCardRows = new HashSet<string>();
        readonly HashSet<string> _visualCaptures132 = new HashSet<string>();
        Settings132 _settings; M1FlowPresenter _presenter; M1RuntimeCoordinator _coordinator;
        StreamWriter _events; string _sourceHash, _failure; int _clicks, _completed;
        bool _isolated; double _lastClick, _lastProgress; string _progressKey;
        readonly TowerAutoPlayerSoak110.ButtonReadiness122 _ready = new TowerAutoPlayerSoak110.ButtonReadiness122();
        CampaignState State132() => (CampaignState)CampaignField.GetValue(_coordinator);

        IEnumerator Start()
        {
            Application.runInBackground = true; _clock.Start();
            var routine = Run132();
            while (true)
            {
                object next = null; bool advanced;
                try { advanced = routine.MoveNext(); if (advanced) next = routine.Current; }
                catch (Exception e) { _failure = e.ToString(); break; }
                if (!advanced) break;
                yield return next;
            }
            Application.logMessageReceived -= OnLog132;
            if (_settings != null)
            {
                bool sourceUntouched = Hash132(_settings.Source) == _sourceHash;
                bool passed = _isolated && _failure == null && _errors.Count == 0 && sourceUntouched && _completed >= _settings.Through;
                File.WriteAllText(Path.Combine(_settings.Output,"campaign_loop132_result.json"),JsonConvert.SerializeObject(new {
                    Passed=passed, Failure=_failure, Errors=_errors, ChaptersCompleted=_completed,
                    ThroughChapter=_settings.Through, ActualBattleCount=_battles.Count, ThreeCardRows=_threeCardRows.Count,
                    VisiblePointerClicks=_clicks, Seconds=_clock.Elapsed.TotalSeconds,
                    SourceUntouched=sourceUntouched, PersonalSaveTouched=false, ForcedProgression=false,
                    Driver="Shipping rendered UI raycast/pointer clicks and normal Auto at4x",
                    _settings.Source, _settings.Save, Application.version, Screen.width, Screen.height
                },Formatting.Indented));
                _events?.Dispose();
                UnityEngine.Debug.Log("CAMPAIGN LOOP132 " + (passed?"PASS":"FAIL") + " chapters="+_completed);
                Application.Quit(passed?0:2);
            }
        }
        IEnumerator Run132()
        {
            _settings = Prepared132 ?? throw new InvalidOperationException("Campaign loop was not isolated by boot.");
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Rendered packaged player required.");
            _sourceHash = Hash132(_settings.Source);
            _events = new StreamWriter(Path.Combine(_settings.Output,"campaign_loop132_events.jsonl"));
            Application.logMessageReceived += OnLog132;
            while (_clock.Elapsed.TotalSeconds < 120)
            {
                _presenter = Object.FindObjectsByType<M1FlowPresenter>(FindObjectsSortMode.None).SingleOrDefault();
                _coordinator = _presenter == null ? null : CoordinatorField.GetValue(_presenter) as M1RuntimeCoordinator;
                if (_coordinator != null) break;
                yield return new WaitForSecondsRealtime(.25f);
            }
            Require132(_coordinator != null,"Shipping coordinator did not load.");
            Require132(string.Equals(Path.GetFullPath((string)PathField.GetValue(_coordinator)),Path.GetFullPath(_settings.Save),StringComparison.OrdinalIgnoreCase),"Save isolation mismatch; no clicks issued.");
            _isolated = true; _lastProgress = _clock.Elapsed.TotalSeconds;
            var initial = State132();
            var initialHeroes = initial.Guild.Recruits.Select(r=>r.RecruitId).ToArray();
            var initialItems = initial.Guild.Inventory.Select(i=>i.InstanceId).ToArray();
            var initialRewards = initial.Guild.Development.ClaimedBattleRewardIds.ToArray();
            while (_clock.Elapsed.TotalSeconds < 7200)
            {
                Require132(_errors.Count == 0,"Runtime exception/error during Campaign. See events and player log.");
                var state = State132(); var progress = state.Guild.GuildCity.Strategic017H?.Campaign019;
                var completed = progress?.CompletedChapterIds ?? Array.Empty<string>();
                _completed = Enumerable.Range(1,_settings.Through).Count(n=>completed.Contains("CH018_"+n.ToString("000")));
                if (_completed == _settings.Through && progress?.ActiveOperation == null && state.Guild.GuildCity.PendingEncounter == null && (state.Battle == null || state.Battle.Outcome != BattleOutcome.InProgress && state.Battle.Reward?.Claimed == true)) break;
                var active = progress?.ActiveOperation?.ChapterId ?? "GUILD";
                var board = progress?.Playable020?.WorldGate023?.ActiveOperation;
                var key = active+"|"+_completed+"|"+(board?.ExpeditionDeck089?.PendingReceipt?.ReceiptId ?? "")+"|"+(board?.CurrentNodeId ?? "")+"|"+(progress?.Playable020?.ActiveOperation?.CurrentStepIndex ?? -1)+"|"+(state.Battle?.BattleId ?? "")+"|"+(state.Battle?.Round ?? 0)+"|"+(state.Battle?.Outcome.ToString() ?? "");
                if (_progressKey != key) { _progressKey=key; _lastProgress=_clock.Elapsed.TotalSeconds; Log132(new {Event="progress",Chapter=active,Completed=_completed,State=key,Seconds=_clock.Elapsed.TotalSeconds}); }
                if (_chapterScreens.Add(active)) { StartCoroutine(Capture132(Path.Combine(_settings.Output,"chapter_"+active+".png"))); }
                if (_clock.Elapsed.TotalSeconds-_lastProgress>240)
                {
                    var visible=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled&&b.gameObject.activeInHierarchy).Select(b=>b.name+" | "+b.GetComponentInChildren<Text>()?.text+" | enabled="+b.IsInteractable()).ToArray();
                    throw new InvalidOperationException("Campaign stopped progressing at "+key+". Visible controls: "+string.Join(" ; ",visible));
                }
                var buttons=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled&&b.gameObject.activeInHierarchy&&b.IsInteractable()).ToArray();
                var controller=_presenter.GetComponent<M2BattleExperienceController072>();
                Button nextButton=null;
                if (controller != null && controller.IsActive && state.Battle?.Outcome==BattleOutcome.InProgress)
                {
                    _battles.Add(state.Battle.BattleId);
                    nextButton=buttons.FirstOrDefault(b=>b.name=="Dismiss First Battle Union Coach 076");
                    if (nextButton==null && controller.AnimationSpeed!=4) nextButton=buttons.FirstOrDefault(b=>b.name=="Battle Playback Speed 091");
                    if (nextButton==null && !controller.AutoOrdersEnabled091) nextButton=buttons.FirstOrDefault(b=>b.name=="Battle Auto Orders 091");
                }
                else
                {
                    var blind=buttons.Where(b=>b.name.StartsWith("Blind Quest Card Back ",StringComparison.Ordinal)).OrderBy(b=>b.name).ToArray();
                    if (blind.Length>0)
                    {
                        Require132(board?.ExpeditionDeck089?.CurrentRow.Count==3,"Campaign exploration lacks three actual choices at "+active);
                        _threeCardRows.Add(active+"|"+string.Join("|",board?.ExpeditionDeck089?.CurrentRow.Select(c=>c.CardId)??Array.Empty<string>()));
                        nextButton=blind[(_clicks+_completed)%blind.Length];
                    }
                    else nextButton=buttons.OrderBy(Rank132).ThenBy(b=>b.name,StringComparer.Ordinal).FirstOrDefault(b=>Rank132(b)<1000);
                }
                if (nextButton != null && _clock.Elapsed.TotalSeconds-_lastClick>.5 && _ready.Observe(nextButton,Time.renderedFrameCount))
                {
                    Require132(TowerAutoPlayerSoak110.ButtonReadiness122.TryHit(nextButton,out var pointer,out _,out var diagnostic),"Control not visibly clickable: "+diagnostic);
                    Log132(new {Event="ui_pointer_click",Button=nextButton.name,Label=nextButton.GetComponentInChildren<Text>()?.text,Chapter=active,Seconds=_clock.Elapsed.TotalSeconds});
                    var clickName132=nextButton.name;
                    var clickLabel132=nextButton.GetComponentInChildren<Text>()?.text??"";
                    var visual132=clickName132=="Roll Committed Quest Dice 132"?"quest_dice":
                        clickName132=="Committed Fate Action 132"&&clickLabel132=="ROLL D20"?"d20":
                        clickName132=="Committed Fate Action 132"&&clickLabel132=="SPIN THE WHEEL"?"wheel":
                        clickName132=="Skip Committed Story Glass 132"&&clickLabel132=="ENTER BATTLE"?"story_battle":null;
                    ExecuteEvents.Execute(nextButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                    if(visual132!=null&&_visualCaptures132.Add(visual132))
                    {
                        StartCoroutine(CaptureAfter132(visual132+"_start",0f));
                        StartCoroutine(CaptureAfter132(visual132+"_motion",.7f));
                        StartCoroutine(CaptureAfter132(visual132+"_landing",2.15f));
                    }
                    _clicks++;_lastClick=_clock.Elapsed.TotalSeconds;_ready.Reset();
                }
                yield return new WaitForSecondsRealtime(.15f);
            }
            Require132(_completed==_settings.Through,"Campaign15 runtime deadline reached.");
            var loaded=new AtomicSaveStore().ReadWithRecovery(_settings.Save);
            Require132(loaded.IsSuccess,"Final Campaign save cannot reload.");
            var durable=loaded.Value.CampaignState;
            Require132(initialHeroes.All(id=>durable.Guild.Recruits.Any(r=>r.RecruitId==id)),"Owned hero lost.");
            Require132(initialItems.All(id=>durable.Guild.Inventory.Any(i=>i.InstanceId==id)),"Owned item lost.");
            Require132(initialRewards.All(id=>durable.Guild.Development.ClaimedBattleRewardIds.Contains(id)),"Prior reward receipt lost.");
            Require132(Enumerable.Range(1,_settings.Through).All(n=>durable.Guild.GuildCity.Strategic017H.Campaign019.CompletedChapterIds.Contains("CH018_"+n.ToString("000"))),"Durable chapter completion differs.");
            StartCoroutine(Capture132(Path.Combine(_settings.Output,"chapter15_complete.png")));
            yield return null;
        }
        static int Rank132(Button b)
        {
            var n=b.name; var label=b.GetComponentInChildren<Text>()?.text??"";
            if(n=="Continue From Battle Results 072") return 0;
            if(n=="Committed Fate Action 132"||n=="Roll Committed Quest Dice 132") return 1;
            if(n=="Skip Committed Story Glass 132") return 2;
            if(n.StartsWith("Choose Expedition route card ",StringComparison.Ordinal)) return 3;
            if(n=="Title Primary Play Now 062") return 4;
            if(n.StartsWith("Start playable campaign chapter ",StringComparison.Ordinal)||n.StartsWith("Start compact playable campaign chapter ",StringComparison.Ordinal)) return 5;
            if(n=="Living Guild Hub Primary CTA 074") return 6;
            if(n=="Resume saved campaign deck 131"||n=="Campaign journey travel 131"||n=="Resume Saved Story Interruption 132") return 7;
            if(n=="Return to active quest battle 084"||n=="Flip monster battle tile 084"||n=="Return from completed quest board 084"||n=="Return to campaign battle 084"||n=="Open campaign battle results 084"||n=="Commit campaign battle tile 084") return 8;
            if(n=="Enter optional Expedition card battle 089"||n.StartsWith("Enter saved Expedition",StringComparison.Ordinal)||n.StartsWith("Enter World Gate",StringComparison.Ordinal)) return 9;
            if(n.StartsWith("Return Revealed Quest Card ",StringComparison.Ordinal)||n.StartsWith("Return to blind",StringComparison.Ordinal)||n.StartsWith("Return unseen",StringComparison.Ordinal)) return 15;
            if(label=="CONTINUE QUEST"||label=="CONTINUE CURRENT QUEST"||label=="CONTINUE THIS STORY QUEST"||label=="START THIS STORY QUEST"||label=="ENTER BATTLE"||label=="CLAIM REWARDS & CONTINUE") return 20;
            if(label=="RETURN TO CARDS"||label=="LEAVE CARD") return 30;
            // No retries on errors, debug unlocks, purchases outside a selected
            // card, hidden controls, saves, or combat methods are invoked here.
            return 1000;
        }
        void OnLog132(string message,string stack,LogType kind) { if((kind==LogType.Exception||kind==LogType.Error||kind==LogType.Assert)&&_errors.Count<40) _errors.Add(message+"\n"+stack); }
        IEnumerator CaptureAfter132(string name,float delay)
        {
            if(delay>0)yield return new WaitForSecondsRealtime(delay);
            yield return Capture132(Path.Combine(_settings.Output,"visual_"+name+".png"));
        }
        IEnumerator Capture132(string path)
        {
            yield return new WaitForEndOfFrame();
            var image = new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try { image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { Destroy(image); }
        }
        void Log132(object value) { _events.WriteLine(JsonConvert.SerializeObject(value));_events.Flush(); }
        static void Require132(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
        static string Hash132(string path) { using(var f=File.OpenRead(path)) using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(f)).Replace("-",""); }
    }
}
