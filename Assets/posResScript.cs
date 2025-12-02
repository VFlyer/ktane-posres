using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using UnityEngine;
using KModkit;

public class BaseNInt
{
    private static readonly string baseN = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!@#$%^&*()_-+=|\\{}:;\"'<>,.?/~[]`";
    private static int baseSize = baseN.Length;
    private string v;
    BaseNInt(string value) {v =  value;}

    public static BaseNInt Random()
    {
        return new BaseNInt(String.Join("", Enumerable.Range(0, 20).Select(x => baseN[UnityEngine.Random.Range(0, baseSize)].ToString()).ToArray()));
    }

    public static BaseNInt operator +(BaseNInt a, BaseNInt b)
    {
        string ans = "";
        bool carry = false;
        for (int i = 19; i > -1; i--)
        {
            int digitSum = baseN.IndexOf(a.v[i]) + baseN.IndexOf(b.v[i]) + (carry?1:0);
            carry = digitSum > baseSize-1;
            ans = baseN[digitSum % baseSize] + ans;
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
        List<int> originalList = Enumerable.Range(0, 20).Select(x => baseN.IndexOf(v[x])).ToList();
        List<int> answerList = new List<int>();
        while (originalList.Count > 0)
        {
            int carry = 0;
            var quotient = new List<int>(originalList.Count);

            foreach (int d in originalList)
            {
                int temp = carry * baseSize + d;
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

    private int max(int a, int b){ return a > b ? a : b; }
    
    void refreshStage1()
    {
        Audio.HandlePlaySoundAtTransform(clips[0].name, transform);
        A += X;
        string ans = "";
        ans += BaseNInt.Random().ToString().Substring(0,15) + String.Join("",Enumerable.Range(0, 5).Select(x => " ").ToArray()) + "\n";
        ans += BaseNInt.Random().ToString().Substring(0,16) + String.Join("",Enumerable.Range(0, 4).Select(x => " ").ToArray()) + "\n";
        for (int i=0; i<2; i++) ans+=BaseNInt.Random()+"\n";
        ans += A.ToString();
        for (int i=0; i<4; i++) ans+="\n"+BaseNInt.Random();
        Mess.text = ans;
    }

    IEnumerator state1()
    {
        statusBusy = false;
        int countDown = 1800;
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
                if (second) amount2 = 0;
                else amount1 = 0;
                Center.color= Color.white;
                int timer = 0;
                while (timer < 100)
                {
                    if (state != 2) yield break;
                    if (tapQueued)
                    {
                        Audio.HandlePlaySoundAtTransform(clips[2].name, transform);
                        tapQueued = false;
                        if (second) amount2++; else amount1++;
                        timer = 0;
                    }
                    timer++;
                    ProgressBar.transform.localScale = new Vector3(12*timer/100f, 1, 1);
                    yield return new WaitForSeconds(0.01f);
                }
                ProgressBar.transform.localScale = new Vector3(0, 1, 1);
                if (amount1 < 7 && amount2 < 7)
                {
                    Audio.HandlePlaySoundAtTransform(clips[1].name, transform);
                    if (second)
                    {
                        second = false;
                        print("amount1: " + amount1 +  ", amount2: " + amount2);
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
                    inputtedAnswer = inputtedAnswer.Substring(0, inputtedAnswer.Length - 1);
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
        print("inputtedAnswer = " + inputtedAnswer + ", expected: " + X.toBase36() + "; result = "+ (inputtedAnswer == X.toBase36()));
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
            print(X);
            print(X.toBase36());
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
        
        print(X);
        print(X.toBase36());
        
        Cover.OnInteract += delegate { if (coverBusy) return false; modulePress(); return false;};
        Status.OnInteract += delegate{if (statusBusy) return false; statusPress(); return false;};
    }
}
