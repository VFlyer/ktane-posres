using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KeepCoding;
using UnityEngine;

public class BaseNInt
{
    private static readonly string baseN = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=|\\{}:;\"'<>,.?/~[]`";
    private string v;
    private static int numberBase, amountOfDigits;
    BaseNInt(string value) {v =  value;}

    public static int NumberBase {
        get { return  numberBase; }
        set { numberBase = value; } }

    public static int AmountOfDigits
    {
        get { return  amountOfDigits; }
        set { amountOfDigits = value; }
    }

    public static BaseNInt Random()
    {
        return new BaseNInt(String.Join("", Enumerable.Range(0, amountOfDigits).Select(x => baseN[UnityEngine.Random.Range(0, numberBase)].ToString()).ToArray()));
    }

    public static BaseNInt operator +(BaseNInt a, BaseNInt b)
    {
        string ans = "";
        bool carry = false;
        for (int i = amountOfDigits - 1; i > -1; i--)
        {
            int digitSum = baseN.IndexOf(a.v[i]) + baseN.IndexOf(b.v[i]) + (carry?1:0);
            carry = digitSum > numberBase-1;
            ans = baseN[digitSum % numberBase] + ans;
        }
        return new BaseNInt(ans);
    }

    public static bool operator ==(BaseNInt a, BaseNInt b)
    {
        return a.v == b.v;
    }

    public static bool operator !=(BaseNInt a, BaseNInt b)
    {
        return a.v != b.v;
    }

    public string toBase36()
    {
        List<int> originalList = Enumerable.Range(0, amountOfDigits).Select(x => baseN.IndexOf(v[x])).ToList();
        List<int> answerList = new List<int>();
        while (originalList.Count > 0)
        {
            int carry = 0;
            var quotient = new List<int>(originalList.Count);

            foreach (int d in originalList)
            {
                int temp = carry * numberBase + d;
                int q = temp / 36;
                carry = temp % 36;
                quotient.Add(q);
            }
            answerList.Add(carry);
            originalList = RemoveLeadingZeros(quotient);
        }
        answerList.Reverse();
        return String.Join("", answerList.Select(x => baseN[x].ToString()).ToArray());
    }
    
    private static List<int> RemoveLeadingZeros(List<int> digits)
    {
        int i = 0;
        while (i < digits.Count && digits[i] == 0) i++;
        if (i == digits.Count) return new List<int>();
        return digits.GetRange(i, digits.Count - i);
    }

    public override string ToString()
    {
        return v;
    }
}

public class posResScript : MonoBehaviour
{
    public KMBombModule Module;
    public FakeStatusLight FakeStatusLight;
    public KMSelectable Cover;
    public KMSelectable Status;
    public TextMesh Center;
    public TextMesh Mess;
    public TextMesh ProgressBar;
    public KMAudio Audio;
    public AudioClip[] clips;
    public MeshRenderer modPuzzle;

    static int ModuleIdCounter = 0;
    int ModuleId;
    private BaseNInt X;
    private BaseNInt A;

    private bool coverBusy;
    private bool statusBusy;
    
    private int state = 0;
    private int presses = 5;

    private Color initColor;

    private readonly string tapCodeString = "ABCDE1FGHIJ2LMNOP3QRSTU4VWXYZ567890K";
    private bool tapQueued;
    private string inputtedAnswer = "";
    private bool stopCorouts;

    private bool tpReady, TPWaitForInput;
    
    private int numberBase, amountOfDigits, timer;

    private int max(int a, int b){ return a > b ? a : b; }
    
    void refreshStage1()
    {
        Audio.HandlePlaySoundAtTransform(clips[0].name, transform);
        A += X;
        string ans = "";
        int diff = BaseNInt.AmountOfDigits - 20;
        if (diff > -9)
        {
            ans += BaseNInt.Random().ToString().Substring(0, 15+(diff-1)/2) +
                   String.Join("", Enumerable.Range(0, (diff+11)/2).Select(x => " ").ToArray()) + "\n";
            ans += BaseNInt.Random().ToString().Substring(0, 16+(diff-1)/2) +
                   String.Join("", Enumerable.Range(0, (diff+9)/2).Select(x => " ").ToArray()) + "\n";
        }
        else for (int i=0; i<2; i++) ans+=BaseNInt.Random()+"\n";

        for (int i=0; i<2; i++) ans+=BaseNInt.Random()+"\n";
        ans += A.ToString();
        for (int i=0; i<4; i++) ans+="\n"+BaseNInt.Random();
        Mess.text = ans;
    }

    IEnumerator state1()
    {
        statusBusy = false;
        int countDown = timer;
        tpReady = true;
        while (countDown >= 0 && !stopCorouts)
        {
            if (countDown % 20 == 0) refreshStage1();
            Center.text = countDown / 10 + "." + countDown % 10;
            countDown--;
            yield return new WaitForSeconds(0.1f);
        }

        toState2();
    }

    void toState2()
    {
        Mess.text = "";
        state = 2;
        Center.text = "";
        Center.color= Color.white;
        coverBusy = false;
        StartCoroutine(tapCodeRead());
    }
    
    IEnumerator processAnim1()
    {
        Center.text = "...?";
        yield return new WaitForSeconds(2f);
        Audio.HandlePlaySoundAtTransform(clips[3].name, transform);
        for (int i = 0; i < 41; i++)
        {
            modPuzzle.material.color = Color.Lerp(initColor, initColor / 4f, i / 40f);
            Center.color = Color.Lerp(Color.white, new Color(1,1,1,0.2f), i / 40f);
            Center.text = String.Join("",
                Enumerable.Range(0, 10).Select(x =>
                    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()_-+=|\\{}:;\"'<>,.?/~[]`"[
                        UnityEngine.Random.Range(0, 68)].ToString()).ToArray());
            yield return new WaitForSeconds(.05f);
        }
        Center.text = "Ready?";
        yield return new WaitForSeconds(2f);
        yield return state1();
    }

    void startState1()
    {
        state = 1;
        coverBusy = true;
        statusBusy = true;
        StartCoroutine(processAnim1());
    }

    IEnumerator tapCodeRead()
    {
        
        int amount1 = 0, amount2 = 0;
        bool second = false;
        while (state == 2)
        {
            if (tapQueued)
            {
                TPWaitForInput = true;
                if (second) amount2 = 0;
                else amount1 = 0;
                Center.color= Color.white;
                int timerTC = 0;
                while (timerTC < 100)
                {
                    if (state != 2) yield break;
                    if (tapQueued)
                    {
                        Audio.HandlePlaySoundAtTransform(clips[2].name, transform);
                        tapQueued = false;
                        if (second) amount2++; else amount1++;
                        timerTC = 0;
                    }
                    timerTC++;
                    ProgressBar.transform.localScale = new Vector3(12*timerTC/100f, 1, 1);
                    yield return new WaitForSeconds(0.01f);
                }
                TPWaitForInput = false;
                ProgressBar.transform.localScale = new Vector3(0, 1, 1);
                if (amount1 < 7 && amount2 < 7)
                {
                    Audio.HandlePlaySoundAtTransform(clips[1].name, transform);
                    if (second)
                    {
                        second = false;
                        Debug.LogFormat("[Positive-Resistance #{0}] Inputted ({1},{2}), which is {3}.", ModuleId, amount1, amount2, tapCodeString[6 * (amount1 - 1) + amount2 - 1]);
                        inputtedAnswer += tapCodeString[6 * (amount1 - 1) + amount2 - 1];
                        Center.text = inputtedAnswer.Substring(max(0,inputtedAnswer.Length - 7));
                    }
                    else second = true;
                }
            }
            else if (!second && inputtedAnswer != "")
            {
                int metaTimer = 0;
                while (metaTimer < 256)
                {
                    if (state != 2) yield break;
                    if (tapQueued) break;
                    metaTimer++;
                    Center.color = Color.Lerp(Color.white, Color.red, metaTimer / 256f);
                    ProgressBar.transform.localScale = new Vector3(12*metaTimer/256f, 1, 1);
                    yield return new WaitForSeconds(0.01f);
                }
                ProgressBar.transform.localScale = new Vector3(0, 1, 1);
                if (!tapQueued)
                {
                    inputtedAnswer = inputtedAnswer.Substring(0, max(0,inputtedAnswer.Length - 1));
                    Center.text = inputtedAnswer.Substring(max(0,inputtedAnswer.Length - 7));
                }
            }
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    void modulePress()
    {
        switch (state)
        {
            case 0:
            {
                presses--;
                Center.text = presses.ToString();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
                break;
            }
            case 1: return;
            case 2:
            {
                tapQueued = true;
                break;
            }
        }
    }

    IEnumerator checkThatShi()
    {
        state = 3;
        StopCoroutine(tapCodeRead());
        ProgressBar.transform.localScale = new Vector3(0, 1, 1);
        Center.color = Color.white;
        coverBusy = true;
        statusBusy = true;
        Center.text = "...!";
        Debug.LogFormat("[Positive-Resistance #{0}] Submitted \"{1}\" and expected \"{2}\". {3}.", ModuleId, inputtedAnswer, X.toBase36(), 
            X.toBase36()==inputtedAnswer?"Correct":"Incorrect");
        yield return new WaitForSeconds(2f);
        Audio.HandlePlaySoundAtTransform(clips[4].name, transform);
        for (int i = 0; i < 41; i++)
        {
            modPuzzle.material.color = Color.Lerp(initColor / 4f, initColor, i / 40f);
            Center.text = String.Join("",
                Enumerable.Range(0, 10).Select(x =>
                    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()_-+=|\\{}:;\"'<>,.?/~[]`"[
                        UnityEngine.Random.Range(0, 68)].ToString()).ToArray());
            yield return new WaitForSeconds(.05f);
        }

        if (inputtedAnswer == X.toBase36())
        {
            Module.HandlePass();
            FakeStatusLight.SetPass();
            Center.text = "Positive\nResistance";
        }
        else
        {
            X = BaseNInt.Random();
            A = BaseNInt.Random();
            coverBusy = false;
            statusBusy = false;
            tpReady = false;
            Debug.LogFormat("[Positive-Resistance #{0}] Your new answer is \"{1}\" (\"{2}\" in base-36).", ModuleId, X, X.toBase36());
            state = 0;
            presses = 5;
            Center.text = "5";
            if (inputtedAnswer != "") Module.HandleStrike();
            else Audio.HandlePlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
            FakeStatusLight.FlashStrike();
            inputtedAnswer = "";
            stopCorouts = false;
        }
    }

    void statusPress()
    {
        switch (state)
        {
            case 0:
            {
                if (presses == 0)
                {
                    FakeStatusLight.SetPass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                }

                if (presses < 0)
                {
                    FakeStatusLight.SetInActive();
                    startState1();
                }
                break;
            }
            case 1:
            {
                stopCorouts = true;
                break;
            }
            case 2:
            {
                StartCoroutine(checkThatShi());
                break;
            }
        }
    }

    void Awake()
    {
        
        ModConfig<PosresSettings> modConfig = new ModConfig<PosresSettings>("PosresSettings");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;
        TryOverrideMission();
        numberBase = Settings.numberBase < 2 || Settings.numberBase > 94?94:Settings.numberBase;
        amountOfDigits = Settings.amountOfDigits < 1 || Settings.amountOfDigits > 20?20:Settings.amountOfDigits;
        timer = Settings.timer < 10?1800:Settings.timer;
        
        BaseNInt.NumberBase = numberBase;
        BaseNInt.AmountOfDigits = amountOfDigits;
        
        Mess.text = "";
        Center.text = "5";
        ProgressBar.transform.localScale = new Vector3(0, 1, 1);
        initColor = modPuzzle.material.color;
        ModuleId = ModuleIdCounter++;
        ModuleId++;
        FakeStatusLight = Instantiate(FakeStatusLight);
        FakeStatusLight.GetStatusLights(transform);
        FakeStatusLight.Module = Module;
        X = BaseNInt.Random();
        A = BaseNInt.Random();
        
        Debug.LogFormat("[Positive-Resistance #{0}] Base-{1}, {2} digits.", ModuleId, BaseNInt.NumberBase, BaseNInt.AmountOfDigits);
        Debug.LogFormat("[Positive-Resistance #{0}] Your answer is \"{1}\" (\"{2}\" in base-36).", ModuleId, X, X.toBase36());
        
        Cover.OnInteract += delegate { if (coverBusy) return false; modulePress(); return false;};
        Status.OnInteract += delegate{if (statusBusy) return false; statusPress(); return false;};
    }
    
    
    private PosresSettings Settings = new PosresSettings();
	
    void TryOverrideMission()
    {
        var desc = Game.Mission.Description ?? "";
        Match regexMatchCountVariants = Regex.Match(desc, @"\[Positive-Resistance\]\s(\d+),(\d+),(\d+)");
        if (!regexMatchCountVariants.Success) return;
        int?[] valueMatches = Enumerable.Range(1,3).Select(x => regexMatchCountVariants.Groups[x].Value.TryParseInt()).ToArray();
        if (valueMatches[0] != null) Settings.numberBase = valueMatches[0].Value;
        if (valueMatches[1] != null) Settings.amountOfDigits = valueMatches[1].Value;
        if (valueMatches[2] != null) Settings.timer = valueMatches[2].Value;
    }
	
    class PosresSettings
    {
        public int numberBase = 94;
        public int amountOfDigits = 20;
        public int timer = 1800;
    }

    static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "PosresSettings.json" },
            { "Name", "Positive-Resistance Settings" },
            { "Listings", new List<Dictionary<string, object>>{
                new Dictionary<string, object>
                {
                    { "Key", "numberBase" },
                    { "Text", "Base" },
                    { "Description", "Base of the numbers. Default and maximum is 94." }
                },
                new Dictionary<string, object>
                {
                    { "Key", "amountOfDigits" },
                    { "Text", "Size" },
                    { "Description", "Amount of digits in the base-N number. Default and maximum is 20." }
                },
                new Dictionary<string, object>
                {
                    { "Key", "timer" },
                    { "Text", "Timer" },
                    { "Description", "Time restriction of Read mode in deciseconds. Default is 1800." }
                },
            } }
        }
    };
    

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Use !{0} 1-6 to press module 1-6 times. Use !{0} S to press status light. You can use multiple commands at once, for example: !{0} 12 22 31S.";
    private bool TwitchShouldCancelCommand;
#pragma warning restore 414
    
    public IEnumerator ProcessTwitchCommand (string Command) {
        string comm = Command.ToUpper();
        foreach(char c in comm) {if (!"123456 S".Contains(c))
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }}
        yield return null;
        foreach (char c in comm)
        {
            switch (c)
            {
                case 'S':
                {
                    Status.OnInteract();
                    if (state == 2)
                    {
                        yield return "solve";
                        yield return "strike";
                    }
                    break;
                }
                case ' ':
                {
                    break;
                }
                case '1': case '2': case '3': case '4': case '5': case '6':
                {
                    for (int i = 0; i < c - '0'; i++)
                    {
                        Cover.OnInteract();
                        yield return new WaitForSeconds(.1f);
                    }
                    break;
                }
            }
            yield return new WaitWhile(() => !TwitchShouldCancelCommand && TPWaitForInput);
            if (TwitchShouldCancelCommand)
            {
                yield return "cancelled";
                yield break;
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve() {
        yield return null;
        if (state == 0)
        {
            for (int i=0; i<6; i++) Cover.OnInteract();
            Status.OnInteract();
            while (state == 0) yield return new WaitForSeconds(.1f);
        }

        if (state == 1)
        {
            while(!tpReady) yield return new WaitForSeconds(.1f);
            Status.OnInteract();
            yield return new WaitForSeconds(1f);
        }

        if (state == 2)
        {
            inputtedAnswer = "";
            foreach (char c in X.toBase36())
            {
                int amount1 = tapCodeString.IndexOf(c)/6+1, amount2 =  tapCodeString.IndexOf(c)%6+1;
                for (int i = 0; i < amount1; i++)
                {
                    Cover.OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
                yield return new WaitWhile(() => TPWaitForInput);
                for (int i = 0; i < amount2; i++) {
                    Cover.OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
                yield return new WaitWhile(() => TPWaitForInput);
            }
            Status.OnInteract();
        }
    }
}
