using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Completely_random_slot;

public class MainPage : ContentPage
{
    private int currentStage = 3;   //桁数
    private int maxDigit = 9;       //通常は9(0〜9)、7当選時は7(0〜7)
    private int minDigit = 1;
    private int coins = 100;
    private int remainingSpins = -1;//残り回転数(-1は無制限)

    private Random random = new Random();
    private bool isSpinning = false;
    private int previousNumber = -1; //前回の出目を記録

    //タップされるまで待つためのオブジェクト
    private TaskCompletionSource<bool>? tapCompletionSource;

    private readonly string[] LuckyNumbersStage3 = {
        "001", "007", "012","081",
        "100", "123",
        "210", "234",
        "314", "321", "345","365",
        "432", "456",
        "514", "543", "567",
        "654", "678",
        "765", "789",
        "876",
        "987"
        };
    private readonly string[] LuckyNumbersStage4 = {
        "0123", "0706", "0906",
        "1000", "1221", "1234",
        "2345",
        "3210", "3456",
        "4321", "4567", "4649", "4989",
        "5432", "5678",
        "6543", "6789",
        "7654",
        "8765",
        "9876",
        };
    private readonly string[] LuckyNumbersStage5 = {
        "01234",
        "10000", "12345",
        "23456",
        "34567",
        "43210", "45678",
        "54321", "56789",
        "65432",
        "76543",
        "87654",
        "98765",
        };

    private readonly int[] SubLuckyNumbers1 = { 7 };
    private readonly int[] SubLuckyNumbers2 = { 17, 19, 39, 77 };
    private readonly int[] SubLuckyNumbers3 = { 123, 321, 777 };

    private bool isChinChiroMode = false;
    private int chinchiroStartCoins = 0;

    private enum ChinChiroHand
    {
        PinZorome,
        Zorome,
        Shigoro,
        Hifumi,
        NomalEye,
        YakuNashi
    }

    private Label titleLabel;
    private Label coinLabel;
    private Label stageLabel;
    private Label displayLabel;
    private Label statusLabel;
    private Label reachLabel;
    private Grid overlayGrid;
    private Label overlayLabel;
    private Button spinButton;

    public MainPage()
    {
        BackgroundColor = Color.FromArgb("#1E1E2E");//ダークモード風背景

        titleLabel = new Label
        {
            Text = "完全ランダムスロット",
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Gold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 15, 0, 0)
        };

        coinLabel = new Label
        {
            Text = "所持コイン : 100",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Orange,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 5)
        };

        stageLabel = new Label
        {
            Text = "STAGE 1 : 1/100(1%)",
            FontSize = 14,
            TextColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };

        displayLabel = new Label
        {
            Text = "[ 0 | 0 | 0 ]",
            FontSize = 40,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 15)
        };

        statusLabel = new Label
        {
            Text = "ボタンを押してスタート!",
            FontSize = 15,
            TextColor = Colors.SkyBlue,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 5)
        };

        reachLabel = new Label
        {
            Text = "リーチ！！",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Red,
            HorizontalOptions = LayoutOptions.Center,
            TranslationX = -400,
            IsVisible = false
        };

        spinButton = new Button
        {
            Text = "SPIN",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.Crimson,
            TextColor = Colors.White,
            CornerRadius = 15,
            HeightRequest = 60,
            WidthRequest = 200,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 15)
        };
        spinButton.Clicked += OnSpinClicked;

        //画面全体を覆い隠す全画面オーバーレイレイヤー
        overlayGrid = new Grid
        {
            IsVisible = false,
            BackgroundColor = Colors.Transparent, //画面を隠す濃い背景
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = false //演出中にボタンなどを押さないようにガード
        };

        //画面を横断する大量の数字ラベル
        overlayLabel = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Goldenrod,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TranslationX = -1500,
        };
        overlayGrid.Children.Add(overlayLabel);

        var mainLayout = new VerticalStackLayout
        {
            Padding = new Thickness(20),
            Spacing = 5,
            Children =
            {
                titleLabel,
                coinLabel,
                stageLabel,
                displayLabel,
                statusLabel,
                reachLabel,
                spinButton
            }
        };

        //画面全体のどこをタップしても反応するやつを追加
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnScreenTapped;
        mainLayout.GestureRecognizers.Add(tapGesture);

        //メイン画面の上に全画面レイヤー(overlayGrid)を重ねる
        var rootGrid = new Grid();
        rootGrid.Children.Add(mainLayout); //奥:通常のゲーム画面
        rootGrid.Children.Add(overlayGrid);//手前:画面を覆い隠す全画面レイヤー

        Content = rootGrid;

        UpdateHeaderUI();
    }

    private void UpdateHeaderUI()
    {
        coinLabel.Text = $"所持コイン: {coins}";

        if (coins < 0)
        {
            coinLabel.TextColor = Colors.Red;
        }
        else
        {
            coinLabel.TextColor = Colors.Orange;
        }

        string spinText = (remainingSpins < 0) ? "無制限" : $"{remainingSpins}回";
        string rangeText = $"0~{maxDigit}";
        string probText = GetProbabilityText(currentStage, maxDigit);

        stageLabel.Text = $"STAGE{currentStage - 2} : {probText} (範囲: {rangeText} / 残り: {spinText})";
    }

    private async void OnSpinClicked(object sender, EventArgs e)
    {
        if (isSpinning) return;

        if (remainingSpins == 0)
        {
            if (isChinChiroMode)
            {
                isChinChiroMode = false;
                minDigit = 0;
                maxDigit = 9;
                remainingSpins = (currentStage == 3) ? -1 : 100;

                int totalGain = coins - chinchiroStartCoins;

                string title = "🎰 チンチロ BONUS RESULT 🎰";
                string resultMessage = "";

                if (totalGain > 0)
                {
                    resultMessage = $" TOTAL WIN \n\n 【 + {totalGain} Coin】\n\n YOU WIN!! CONGRATULATIONS!!";
                }
                else if (totalGain == 0)
                {
                    resultMessage = $" DRAW \n\n 【 ± 0 Coin】";
                }
                else
                {
                    resultMessage = $" TOTAL LOSE \n\n 【 {totalGain} Coin】\n\n YOU LOSE...";
                }

                UpdateHeaderUI();
                await DisplayAlert(title, resultMessage, "通常モードへ戻る");

                isSpinning = false;
                spinButton.IsEnabled = true;
                return;
            }
            await DemoteStageAsync();
            return;
        }

        isSpinning = true;
        spinButton.IsEnabled = false;

        if (remainingSpins > 0)
        {
            remainingSpins--;
        }

        coins--;

        UpdateHeaderUI();

        statusLabel.Text = "抽選中…";
        statusLabel.TextColor = Colors.Yellow;

        //0.1%の確率でステージアップ！
        bool isPremiumNotice = (random.Next(1000) == 0);
        int forcedZoromeDigit = -1;

        if (isPremiumNotice)
        {
            //当てるゾロ目の数字をランダムで事前に決定
            forcedZoromeDigit = random.Next(minDigit, maxDigit + 1);

            //大量の数字が駆け抜ける演出を実行
            await PlayNumberStreamAnimationAsync();
        }

        //1%の確率でラッキー確定！
        bool isYellowNotice = (random.Next(100) == 0);
        Color originalBgColor = BackgroundColor;

        string forcedLuckyNum = "";
        if (isYellowNotice)
        {
            BackgroundColor = Color.FromArgb("#856404");

            //当てるラッキーナンバーを一つ選出
            forcedLuckyNum = GetRandomLuckyNumberForStage(currentStage);
        }

        int[] result = new int[currentStage];

        //左から順にリールを止めるアニメーション
        for (int stoppedReels = 0; stoppedReels < currentStage; stoppedReels++)
        {
            //リーチの判定
            bool isReach = false;
            if (stoppedReels == currentStage - 1)
            {
                isReach = true;
                int firstDigit = result[0];
                for (int k = 1; k < currentStage - 1; k++)
                {
                    if (result[k] != firstDigit)
                    {
                        isReach = false;
                        break;
                    }
                }
            }

            //リーチ成立後のカットイン演出
            if (isReach)
            {
                statusLabel.Text = "リーチ！";
                statusLabel.TextColor = Colors.Red;
                Task reachTask = PlayReachAnimationAsync();

                //アニメーションが終わるまでリールを回転させ続ける
                while (!reachTask.IsCompleted)
                {
                    for (int d = stoppedReels; d < currentStage; d++)
                    {
                        result[d] = random.Next(minDigit, maxDigit + 1);
                    }
                    UpdateDisplay(result);
                    await Task.Delay(50);
                }
            }

            //リーチの回転数にタメを作る
            int spinCount = isReach ? 20 : 8; //8回→25回
            int spinDelay = isReach ? 100 : 50; //50ms→80ms

            //1つのリールが止まるまでの回転時間(8回 × 50ms = 0.4秒回転)
            for (int i = 0; i < spinCount; i++)
            {
                //まだ止まっていない右側のリールだけをランダムに更新
                for (int d = stoppedReels; d < currentStage; d++)
                {
                    result[d] = random.Next(minDigit, maxDigit + 1);
                }
                UpdateDisplay(result);
                await Task.Delay(spinDelay);
            }

            //黄色予告のときは止まったリールの数字をラッキーナンバーに置き換える
            if (isYellowNotice)
            {
                result[stoppedReels] = forcedLuckyNum[stoppedReels] - '0';
                UpdateDisplay(result);
            }
            else if (isPremiumNotice)
            {
                result[stoppedReels] = forcedZoromeDigit;
                UpdateDisplay(result);
            }
        }

        bool isZorome = result.All(x => x == result[0]);
        int numberVal = ConvertArrayToNumber(result);
        bool isLucky = IsMainLuckyNumber(result, currentStage);
        if (isChinChiroMode)
        {
            var chinchiroHand = EvaluateChinChiro(result);

            switch (chinchiroHand)
            {
                case ChinChiroHand.PinZorome:
                    int PinCoin = 1000 * (currentStage - 2) * 5;
                    coins += PinCoin;
                    statusLabel.Text = $"ピンゾロ(1)!!　5倍!(+{PinCoin} Coin)";
                    statusLabel.TextColor = Colors.Gold;
                    await PlayZoromeAnimationAsync();
                    break;
                case ChinChiroHand.Zorome:
                    int hitDigit = result[0];
                    int zoromeCoin = (hitDigit + 1) * 100 * (currentStage - 2) * 3;
                    coins += zoromeCoin;
                    statusLabel.Text = $"ゾロ目({hitDigit})!　3倍(+{zoromeCoin} Coin)";
                    statusLabel.TextColor = Colors.Lime;
                    await PlayZoromeAnimationAsync();
                    break;
                case ChinChiroHand.Shigoro:
                    int ShigoroCoin = 1000 * (currentStage - 2) * 2;
                    coins += ShigoroCoin;
                    statusLabel.Text = $"シゴロ!　2倍!(+{ShigoroCoin} Coin)";
                    statusLabel.TextColor = Colors.Goldenrod;
                    break;
                case ChinChiroHand.NomalEye:
                    coins += 1;
                    statusLabel.Text = $"通常の目　掛け金返還(±0 Coin)";
                    statusLabel.TextColor = Colors.Goldenrod;
                    break;
                case ChinChiroHand.Hifumi:
                    int hifumiPenalty = 500 * (currentStage - 2) * 2;
                    coins -= hifumiPenalty;
                    statusLabel.Text = $"ヒフミ！　2倍没収！(-{hifumiPenalty} Coin)";
                    statusLabel.TextColor = Colors.Purple;
                    break;
                case ChinChiroHand.YakuNashi:
                    statusLabel.Text = "役無し…　ハズレ！";
                    statusLabel.TextColor = Colors.Salmon;
                    break;
            }
        }
        //ゾロ目判定
        else if (isZorome)
        {
            int hitDigit = result[0];
            int rewardCoin = (hitDigit + 1) * 100 * (currentStage - 2);
            coins += rewardCoin;

            statusLabel.Text = $"大当たり！！({hitDigit}のゾロ目！ +{rewardCoin} Coin)";
            statusLabel.TextColor = Colors.Lime;

            await PlayZoromeAnimationAsync();
            await Task.Delay(3000);

            //当たった時点での正確な確率テキストを取得
            string currentProbText = GetProbabilityText(currentStage, maxDigit);

            if (hitDigit == 7)
            {
                maxDigit = 7;
            }
            else if (hitDigit == 6)
            {
                minDigit = 1;
                maxDigit = 6;
                remainingSpins = 10;
                isChinChiroMode = true;
                chinchiroStartCoins = coins;

                await DisplayAlert("チンチロモード突入！",
                "出目が1〜6限定&10回フリースピン！\n" +
                "・ピンゾロ(111):5倍配当\n" +
                "・ゾロ目:3倍配当\n" +
                "・シゴロ:2倍配当\n" +
                "・通常の目:賭けコイン返還\n" +
                "・役無し:賭けたコイン没収\n" +
                "・ヒフミ:2倍没収\n", "勝負開始！");
            }
            else
            {
                minDigit = 0;
                maxDigit = 9;
                isChinChiroMode = false;
            }

            if (hitDigit != 6)
            {
                if (currentStage == 3)
                {
                    await DisplayAlert("突破！", $"{currentProbText}を達成しました" + (maxDigit == 7 ? "\n★7ボーナス:次回は0〜7のみ出現！" : ""), "次のステージへ移行します");
                    currentStage = 4;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0 });
                }
                else if (currentStage == 4)
                {
                    await DisplayAlert("突破！", $"{currentProbText}を達成しました" + (maxDigit == 7 ? "\n★7ボーナス:次回は0〜7のみ出現！" : ""), "次のステージへ移行します");
                    currentStage = 5;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0, 0 });
                }
                else
                {
                    await DisplayAlert("全制覇！", $"{currentProbText}を達成。見事だ。", "次の挑戦を待っている");
                    ResetToStage1();
                }
            }
            UpdateHeaderUI();
        }
        //ラッキーナンバーの処理(ラッキースロット突入)
        else if (isLucky)
        {


            statusLabel.Text = $"ラッキーナンバー達成({numberVal})!　サブスロット突入";
            statusLabel.TextColor = Colors.Cyan;
            UpdateHeaderUI();

            await WaitForTapAsync();

            //サブスロット処理実行
            await ProcessSubSlotAsync(result);
        }
        else
        {
            statusLabel.Text = "残念…ハズレ！";
            statusLabel.TextColor = Colors.Salmon;

            if (remainingSpins == 0)
            {
                await Task.Delay(5000);
                await DemoteStageAsync();
            }
        }

        //背景色を元に戻す
        BackgroundColor = originalBgColor;

        //2回連続同じ出目の判定
        int currentNumber = ConvertArrayToNumber(result);

        if (previousNumber != -1 && currentNumber == previousNumber)
        {
            if (coins != 0)
            {
                long squaredCoins = (long)coins * coins;

                //intの上限(約21億)を超えないよう制限
                coins = (int)Math.Min(squaredCoins, int.MaxValue);

                statusLabel.Text = $"2連続同数({currentNumber})達成！　所持コインが2乗({coins})！！";
                statusLabel.TextColor = Colors.Goldenrod;

                //ド派手に拡大演出
                await displayLabel.ScaleTo(2.0, 300);
                await displayLabel.ScaleTo(1.0, 300);

                await Task.Delay(5000);
            }
        }
        //今回の出目を前回の出目として記録
        previousNumber = currentNumber;

        UpdateHeaderUI();
        isSpinning = false;
        spinButton.IsEnabled = true;
    }

    private void OnScreenTapped(object? sender, EventArgs e)
    {
        //タップ待ち状態なら待ちを解除して次へ進める
        if (tapCompletionSource != null && !tapCompletionSource.Task.IsCompleted)
        {
            tapCompletionSource.TrySetResult(true);
        }
        //スピン中でなければ画面タップでSPINを開始する
        else if (!isSpinning)
        {
            OnSpinClicked(this, EventArgs.Empty);
        }
    }

    //プレイヤーが画面をタップするまで処理を一時停止するメソッド
    private async Task WaitForTapAsync()
    {
        tapCompletionSource = new TaskCompletionSource<bool>();
        await tapCompletionSource.Task;
        tapCompletionSource = null;
    }

    private async Task DemoteStageAsync()
    {
        if (currentStage == 5)
        {
            currentStage = 4;
            remainingSpins = 100;
            previousNumber = -1;
            maxDigit = 9;
            UpdateDisplay(new int[] { 0, 0, 0, 0 });
            UpdateHeaderUI();
            await DisplayAlert("↓Down...↓", "回転数が0になったためステージが1段落ちます", "次へ");
        }
        else if (currentStage == 4)
        {
            currentStage = 3;
            remainingSpins = -1;
            previousNumber = -1;
            maxDigit = 9;
            UpdateDisplay(new int[] { 0, 0, 0 });
            UpdateHeaderUI();
            await DisplayAlert("↓Down...↓", "回転数が0になったため最初のステージに移行します", "Re:TrymenT");

        }
    }

    //リーチ演出
    private async Task PlayReachAnimationAsync()
    {
        reachLabel.IsVisible = true;
        reachLabel.TranslationX = -400;

        //左外から画面中央へスライドイン
        await reachLabel.TranslateTo(0, 0, 300, Easing.CubicOut);

        //画面中央へ一時停止
        await Task.Delay(500);

        //右外へスライドアウト
        await reachLabel.TranslateTo(400, 0, 300, Easing.CubicIn);

        reachLabel.IsVisible = false;
        reachLabel.TranslationX = -400;
    }

    private ChinChiroHand EvaluateChinChiro(int[] result)
    {
        //数字を小さい順に並べ替える
        var sorted = result.OrderBy(x => x).ToArray();
        int len = sorted.Length;

        //ゾロ目
        if (sorted.All(x => x == sorted[0]))
        {
            if (sorted[0] == 1) return ChinChiroHand.PinZorome; //ピンゾロ
            return ChinChiroHand.Zorome;//通常ゾロ目
        }

        //連番かどうかを判定するための関数
        bool IsSequential(int[] arr)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                if (arr[i + 1] - arr[i] != 1) return false;
            }
            return true;
        }

        //シゴロ
        if (IsSequential(sorted) && sorted[len - 1] == 6)
        {
            return ChinChiroHand.Shigoro;
        }

        //ヒフミ
        if (IsSequential(sorted) && sorted[0] == 1)
        {
            return ChinChiroHand.Hifumi;
        }

        //通常の目
        if (sorted.Distinct().Count() < len)
        {
            return ChinChiroHand.NomalEye;
        }

        //役無し
        return ChinChiroHand.YakuNashi;
    }

    private async Task PlayZoromeAnimationAsync()
    {
        Color originalColor = displayLabel.TextColor;

        //数字を大きくする
        await displayLabel.ScaleTo(1.6, 200, Easing.CubicOut);

        //画面シェイク&虹色フラッシュ
        Color[] rainbowColors = new Color[]
        {
            Colors.Gold, Colors.Crimson, Colors.Cyan, Colors.Lime, Colors.Magenta, Colors.Yellow
        };

        for (int i = 0; i < 15; i++)
        {
            //文字をランダムな派手色に変更
            displayLabel.TextColor = rainbowColors[i % rainbowColors.Length];

            //画面全体を左右に揺らす
            double offsetX = (i % 2 == 0) ? 12 : -12;
            await Content.TranslateTo(offsetX, 0, 40);
        }

        //画面位置を中央に戻す
        await Content.TranslateTo(0, 0, 50);

        //数字が跳ねながら元の位置に戻る
        await displayLabel.ScaleTo(1.0, 400, Easing.BounceOut);

        //文字の色を元に戻す
        displayLabel.TextColor = originalColor;
    }

    private async Task PlayNumberStreamAnimationAsync()
    {
        statusLabel.Text = "???";
        statusLabel.TextColor = Colors.Gold;

        overlayLabel.FontSize = 50;

        //画面を横切るランダムな大量の数字列を作成
        overlayGrid.IsVisible = true;
        overlayLabel.TranslationX = -2000; //画面左

        bool isStreaming = true;
        int[] tempReelDigits = new int[currentStage]; //リール回転用の配列

        //数字が流れる
        Task shuffleTask = Task.Run(async () =>
        {
            while (isStreaming)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int row = 0; row < 30; row++)
                {
                    string line = string.Join(" ", Enumerable.Range(0, 120).Select(_ => random.Next(0, 10)));
                    sb.AppendLine(line);
                }

                //リール用のランダムな数字
                for (int d = 0; d < currentStage; d++)
                {
                    tempReelDigits[d] = random.Next(minDigit, maxDigit + 1);
                }

                //メインUIスレッドでテキスト更新
                Dispatcher.Dispatch(() =>
                {
                    overlayLabel.Text = sb.ToString();
                    UpdateDisplay(tempReelDigits);
                });

                await Task.Delay(50);
            }
        });

        //3.5秒かけて画面左から右に流れる
        await overlayLabel.TranslateTo(1500, 0, 3500, Easing.Linear);

        //シャッフル終了
        isStreaming = false;
        await shuffleTask;

        //元に戻す
        overlayGrid.IsVisible = false;
        overlayLabel.TranslationX = -2000;
    }

    //サブスロット演出・処理
    private async Task ProcessSubSlotAsync(int[] mainResult)
    {
        int subLanes = currentStage - 2;
        bool canSpinAgain = false;

        do
        {
            canSpinAgain = false;
            statusLabel.Text = $"サブスロット挑戦中";
            statusLabel.TextColor = Colors.Magenta;

            int[] subResult = new int[subLanes];

            //サブスロット回転
            for (int stopped = 0; stopped < subLanes; stopped++)
            {
                for (int i = 0; i < 15; i++)
                {
                    for (int d = stopped; d < subLanes; d++)
                    {
                        subResult[d] = random.Next(0, 10);
                    }
                    displayLabel.Text = "[ " + string.Join(" | ", subResult) + " ]";
                    await Task.Delay(50);
                }
            }



            int subVal = ConvertArrayToNumber(subResult);
            bool subLucky = IsSubLuckyNumber(subVal, subLanes);

            int gainCoin = subVal;
            coins += gainCoin;

            int recoverySpins = subVal; //制限回数回復量
            if (remainingSpins >= 0)
            {
                remainingSpins += recoverySpins;
            }

            if (subLucky)
            {
                statusLabel.Text = $"フリースピンリトリガー({subVal})! +{recoverySpins}回 & +{gainCoin} Coin & もう一回！";
                statusLabel.TextColor = Colors.Gold;
                await WaitForTapAsync();
                UpdateHeaderUI();
                canSpinAgain = true;
            }
            else
            {
                statusLabel.Text = $"サブスロット結果({subVal})! +{recoverySpins}回、 +{gainCoin} Coin獲得!";
                statusLabel.TextColor = Colors.Lime;
                await Task.Delay(2000);
                UpdateHeaderUI();
            }
        } while (canSpinAgain);

        UpdateDisplay(mainResult);
    }

    private bool IsMainLuckyNumber(int[] result, int stage)
    {
        string resultStr = string.Join("", result);

        if (stage == 3) return LuckyNumbersStage3.Contains(resultStr);

        if (stage == 4)
        {
            string yearStr = DateTime.Now.ToString("yyyy");
            string todayStr = DateTime.Now.ToString("MMdd");
            string timeStr = DateTime.Now.ToString("HHmm");


            return LuckyNumbersStage4.Contains(resultStr) || resultStr == yearStr || resultStr == todayStr || resultStr == timeStr;
        }

        if (stage == 5) return LuckyNumbersStage5.Contains(resultStr);
        return false;
    }

    private bool IsSubLuckyNumber(int num, int subLanes)
    {
        if (subLanes == 1) return SubLuckyNumbers1.Contains(num);
        if (subLanes == 2) return SubLuckyNumbers2.Contains(num);
        if (subLanes == 3) return SubLuckyNumbers3.Contains(num);
        return false;
    }

    private string GetRandomLuckyNumberForStage(int stage)
    {
        if (stage == 3)
        {
            int index = random.Next(LuckyNumbersStage3.Length);
            return LuckyNumbersStage3[index];
        }
        if (stage == 4)
        {
            int index = random.Next(LuckyNumbersStage4.Length);
            return LuckyNumbersStage4[index];
        }
        if (stage == 5)
        {
            int index = random.Next(LuckyNumbersStage5.Length);
            return LuckyNumbersStage5[index];
        }
        return "000";
    }

    private string GetProbabilityText(int stage, int digit)
    {
        if (digit == 7)
        {
            if (stage == 3) return "1/64(約1.56%)";
            if (stage == 4) return "1/512(約0.2%)";
            if (stage == 5) return "1/4096(約0.024%)";
        }
        else
        {
            if (stage == 3) return "1/100(1%)";
            if (stage == 4) return "1/1000(0.1%)";
            if (stage == 5) return "1/10000(0.01%)";
        }
        return "";
    }

    private int ConvertArrayToNumber(int[] arr)
    {
        int num = 0;
        foreach (int d in arr)
        {
            num = num * 10 + d;
        }
        return num;
    }

    private void ResetToStage1()
    {
        currentStage = 3;
        maxDigit = 9;
        remainingSpins = -1;
        previousNumber = -1;
        UpdateDisplay(new int[] { 0, 0, 0 });
        UpdateHeaderUI();
    }

    private void UpdateDisplay(int[] digits)
    {
        displayLabel.Text = "[ " + string.Join(" | ", digits) + " ]";
    }
}