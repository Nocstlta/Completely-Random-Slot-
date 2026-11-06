using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private int minDigit = 0;
    private int coins = 100;
    private int remainingSpins = -1;//残り回転数(-1は無制限)

    private Random random = new Random();
    private bool isSpinning = false;
    private int previousNumber = -1; //前回の出目を記録

    //タップされるまで待つためのオブジェクト
    private TaskCompletionSource<bool> tapCompletionSource;
    
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
        "10000", "11037", "12345",
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
    private int chinchiroPreviousStage = 3;

    private enum ChinChiroHand
    {
        PinZorome,
        Zorome,
        Shigoro,
        Hifumi,
        NomalEye,
        YakuNashi
    }

    private bool isPokerMode = false;
    private int pokerStartCoins = 0;
    private int pokerPreviousStage = 3;
    
    private struct Card
    {
        public int Suit;
        public int Rank;
        
        public Card(int suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }
        
        public override string ToString()
        {
            string[] suits = { "♠", "♥", "♦", "♣" };
            string[] ranks = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"};
            return $"{suits[Suit]}{ranks[Rank]}";
        }
    }

    private enum PokerHand
    {
        RoyalStraightFlush,
        StraightFlush,
        FiveOfAkind,
        FourOfAkind,
        FullHouse,
        Flush,
        Straight,
        ThreeOfAkind,
        TwoPair,
        OnePair,
        NoPair
    }
    
    private int circleStep = 0;
    private bool isSuperRush = false;
    private int superRushSpins = 0;
    private int superRushMultiplier = 10;
    
    private struct ReachData
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
    }
    
    private readonly List<ReachData> BlueReachList = new()
    {
        new ReachData { Text = "📢 「何かが起こる予感……！」", TextColor = Colors.DeepSkyBlue},
        new ReachData { Text = "📢 「気配を感じるぞ…！」", TextColor = Colors.DeepSkyBlue},
        new ReachData { Text = "📢 「何かが、動き出した…？」", TextColor = Colors.DeepSkyBlue}
    };
    
    private readonly List<ReachData> GreenReachList = new()
    {
        new ReachData { Text = "📢 「好機を、感じる…！」", TextColor = Colors.LimeGreen},
        new ReachData { Text = "📢 「悪くない流れだ……いける」", TextColor = Colors.LimeGreen},
        new ReachData { Text = "📢 「流れが来ている…！」", TextColor = Colors.LimeGreen}
    };
    
    private readonly List<ReachData> RedReachList = new()
    {
        new ReachData { Text = "📢 「熱い！熱すぎる展開だ！！」", TextColor = Colors.Red},
        new ReachData { Text = "📢 「止まるな…このまま突き抜けろ！！」", TextColor = Colors.Red},
        new ReachData { Text = "📢 「ねじ込め！ 運命の一撃！！」", TextColor = Colors.Red}
    };
    
    private readonly List<ReachData> GoldReachList = new()
    {
        new ReachData { Text = "📢 「この一撃に、すべてを懸けろ！！」", TextColor = Colors.Gold},
        new ReachData { Text = "📢 「選ばれた者だけが見る景色だ……！！」", TextColor = Colors.Gold},
        new ReachData { Text = "📢 「最高の瞬間が今、訪れる！！」", TextColor = Colors.Gold}
    };

    private Label titleLabel;
    private Label coinLabel;
    private Label stageLabel;
    private Label displayLabel;
    private Label statusLabel;
    private Label reachLabel;
    private Grid overlayGrid;
    private Label overlayLabel;
    private Label miniReachLabel;
    private Label countdownLabel;
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
            Text = "1/100(1%)",
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
            BackgroundColor = Color.FromRgba(0, 0, 0, 200), //画面を隠す濃い背景
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
        
        //左上のミニリーチ表示
        miniReachLabel = new Label
        {
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Gold,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(20),
        };
        
        //中央のカウントダウン用デカ文字
        countdownLabel = new Label
        {
            FontSize = 45,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        
        overlayGrid.Children.Add(miniReachLabel);
        overlayGrid.Children.Add(countdownLabel);

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
        string probText = GetProbabilityText(currentStage, maxDigit);

        stageLabel.Text = $"{probText} / 残り: {spinText}";
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
                
                if (chinchiroPreviousStage == 3)
                {
                    currentStage = 4;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0});
                }
                else if (chinchiroPreviousStage == 4)
                {
                    currentStage = 5;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0, 0});
                }
                else //5桁ステージで当たっていた場合
                {
                    ResetToStage1();
                }
                UpdateHeaderUI();
                
                isSpinning = false;
                spinButton.IsEnabled = true;
                return;
            }
            if (isPokerMode)
            {
                isPokerMode = false;
                minDigit = 0;
                maxDigit = 9;

                int totalGain = coins - pokerStartCoins;
                string title = "🃏 POKER BONUS RESULT 🃏";
                string resultMessage = (totalGain >= 0)
                    ? $"TOTAL WIN \n\n 【 +{totalGain} Coin】"
                    : $"TOTAL LOSE \n\n 【 {totalGain} Coin】";
                UpdateHeaderUI();
                await DisplayAlertAsync(title, resultMessage, "ボーナス終了！");
                
                //ポーカー前のステージに基づいて次のステージへ昇格
                if (pokerPreviousStage == 3)
                {
                    await DisplayAlertAsync("ステージ突破！", "ポーカーボーナス達成！\n次は4桁ステージ（100回制限）です！", "次へ");
                    currentStage = 4;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0});
                }
                else if (pokerPreviousStage == 4)
                {
                    currentStage = 5;
                    remainingSpins = 100;
                    UpdateDisplay(new int[] { 0, 0, 0, 0, 0});
                }
                else //5桁ステージで当たっていた場合
                {
                    await DisplayAlertAsync("全制覇！", "5桁ポーカーボーナス達成！\n最初の3桁ステージに戻ります！", "次へ");
                    ResetToStage1();
                }
                
                UpdateHeaderUI();
                isSpinning = false;
                spinButton.IsEnabled = true;
                return;
            }
            await DemoteStageAsync();
            return;
        }

        isSpinning = true;
        spinButton.IsEnabled = false;
        
        if (isSuperRush)
        {
            superRushSpins--;
        }
        else
        {
            if (remainingSpins > 0) remainingSpins--;
            coins--;
        }

        UpdateHeaderUI();
        
        int nextStep = circleStep;
        
        bool isOneShot = (random.Next(1500) < 1); 
        if (isOneShot)
        {
            nextStep = currentStage;
        }
        else
        {
            int roll = random.Next(100);
            
            double progress = (double)circleStep / 5.0;
            int successRate = Math.Max(5, (int)(25.0 * Math.Cos(progress * (Math.PI / 2.0))));
            
            if (roll < successRate)
            {
                nextStep = circleStep + 1; //成功
            }
            else if (circleStep < currentStage)
            {
                nextStep = 0; //失敗でリセット
            }
        }
        
        // 最後の◯（全マル）がかかっているかのフラグ
        bool isFinalCircleChallenge = (circleStep == currentStage - 1 && nextStep == currentStage);
        
        // 回転中の画面表示用ステップ（全マル達成の時は最後のリールが止まるまで隠しておく）
        int displayStep = isFinalCircleChallenge ? (currentStage - 1) : nextStep;
        
        if (isPokerMode)
        {
            Card[] cards = new Card[5];
            
            for (int stoppedReels = 0; stoppedReels < 5; stoppedReels++)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int d = stoppedReels; d < 5; d++)
                    {
                        cards[d] = GetRandomCard();
                    }
                    UpdatePokerDisplay(cards);
                    await Task.Delay(50);
                }
            }
            var pokerHand = EvaluatePoker(cards);
            int multiplier = pokerPreviousStage - 2;
            
            switch (pokerHand)
            {
                case PokerHand.RoyalStraightFlush:
                    int rfCoin = 10000 * multiplier;
                    coins += rfCoin;
                    statusLabel.Text = $"👑 ロイヤルフラッシュ!! (+{rfCoin} Coin)";
                    await PlayZoromeAnimationAsync();
                    break;
                case PokerHand.StraightFlush:
                    int sfCoin = 5000 * multiplier;
                    coins += sfCoin;
                    statusLabel.Text = $"⚡ ストレートフラッシュ!! (+{sfCoin} Coin)";
                    await PlayZoromeAnimationAsync();
                    break;
                case PokerHand.FiveOfAkind:
                    int fiveCoin = 3000 * multiplier;
                    coins += fiveCoin;
                    statusLabel.Text = $"5カード!! (+{fiveCoin} Coin)";
                    statusLabel.TextColor = Colors.Gold;
                    await PlayZoromeAnimationAsync();
                    break;
                case PokerHand.FourOfAkind:
                    int fourCoin = 1500 * multiplier;
                    coins += fourCoin;
                    statusLabel.Text = $"4カード! (+{fourCoin} Coin)";
                    statusLabel.TextColor = Colors.Goldenrod;
                    break;
                case PokerHand.FullHouse:
                    int fhCoin = 800 * multiplier;
                    coins += fhCoin;
                    statusLabel.Text = $"フルハウス! (+{fhCoin} Coin)";
                    statusLabel.TextColor = Colors.Lime;
                    break;
                case PokerHand.Flush:
                    int flCoin = 600 * multiplier;
                    coins += flCoin;
                    statusLabel.Text = $"フラッシュ! (+{flCoin} Coin)";
                    break;
                case PokerHand.Straight:
                    int stCoin = 500 * multiplier;
                    coins += stCoin;
                    statusLabel.Text = $"ストレート! (+{stCoin} Coin)";
                    statusLabel.TextColor = Colors.Cyan;
                    break;
                case PokerHand.ThreeOfAkind:
                    int threeCoin = 300 * multiplier;
                    coins += threeCoin;
                    statusLabel.Text = $"3カード (+{threeCoin} Coin)";
                    statusLabel.TextColor = Colors.LightGreen;
                    break;
                case PokerHand.TwoPair:
                    int tpCoin = 100 * multiplier;
                    coins += tpCoin;
                    statusLabel.Text = $"ツーペア (+{tpCoin} Coin)";
                    statusLabel.TextColor = Colors.SkyBlue;
                    break;
                case PokerHand.OnePair:
                    int opCoin = 30 * multiplier;
                    coins += opCoin;
                    statusLabel.Text = $"ワンペア (+{opCoin} Coin)";
                    statusLabel.TextColor = Colors.White;
                    break;
                case PokerHand.NoPair:
                    statusLabel.Text = "ノーペア... ハズレ！";
                    statusLabel.TextColor = Colors.Salmon;
                    break;
            }
            
            UpdateHeaderUI();
            isSpinning = false;
            spinButton.IsEnabled = true;
            return;
        }

        statusLabel.Text = isSuperRush ? $"🔥 SUPER RUSH中! (倍率:{superRushMultiplier}倍)" : "抽選中…";
        statusLabel.TextColor = isSuperRush ? Colors.Gold : Colors.Yellow;

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
                bool isNumberReach = true;
                int firstDigit = result[0];
                for (int k = 1; k < currentStage - 1; k++)
                {
                    if (result[k] != firstDigit)
                    {
                        isNumberReach = false;
                        break;
                    }
                }
                // 数字が揃いそうな時だけでなく、最後の◯がかかっている時も強制リーチ！
                isReach = isNumberReach || isFinalCircleChallenge;
            }

            //リーチ成立後のカットイン演出
            if (isReach)
            {
                // 当たり確定予告（黄色予告・プレミアム予告）が出ているか確認
                bool isWinning = isYellowNotice || isPremiumNotice;
                
                // 期待度（色・セリフ）を即座に判定
                ReachData reachEffect = GetReachEffect(isFinalCircleChallenge, isWinning);
                
                // 画面のステータス表示を更新
                statusLabel.Text = reachEffect.Text;
                statusLabel.TextColor = reachEffect.TextColor;
                
                //カットインアニメーション開始
                Task reachTask = PlayReachAnimationAsync();

                //アニメーションが終わるまでリールを回転させ続ける
                while (!reachTask.IsCompleted)
                {
                    for (int d = stoppedReels; d < currentStage; d++)
                    {
                        result[d] = random.Next(minDigit, maxDigit + 1);
                    }
                    UpdateDisplayWithCircles(result, displayStep);
                    await Task.Delay(50);
                }
                
                if (reachEffect.TextColor == Colors.Red || reachEffect.TextColor == Colors.Gold)
                {
                    // 赤または金リーチのとき、一定確率（例：40%）でこの激アツ演出に発展！
                    if (random.Next(100) < 40)
                    {
                        int targetDigit = result[0]; // 揃えようとしているテンパイ数字
                        
                        // 今回の抽選で本当に当たるかどうか（黄色予告・プレミアム、または自力当選か）
                        bool isWin = isWinning || (random.Next(100) < 15);
                        
                        // 特殊演出を実行して、結果（true=当たり, false=ハズレ）を受け取る
                        bool actualWin = await PlaySpecialCountdownReachAsync(targetDigit, isWin);
                        
                        // 演出の結果に合わせて、最後のリールの数字を強制的に決定する
                        if (actualWin)
                        {
                            result[currentStage - 1] = targetDigit; // 見事揃う！
                        }
                        else
                        {
                            // 外れの場合：テンパイ数字の次の数字（例: 1なら2）にしてハズス
                            result[currentStage - 1] = (targetDigit >= maxDigit) ? minDigit : targetDigit + 1;
                        }
                        
                        // 画面を最終結果に更新して、通常の「最後の通常回転ループ」をスキップして判定へ飛ぶ！
                        UpdateDisplayWithCircles(result, displayStep);
                        break;
                    }
                }
            }

            //リーチの回転数にタメを作る
            int spinCount = isReach ? 25 : 13; //8回→25回
            int spinDelay = isReach ? 150 : 50; //50ms→80ms

            //1つのリールが止まるまでの回転時間(8回 × 50ms = 0.4秒回転)
            for (int i = 0; i < spinCount; i++)
            {
                //まだ止まっていない右側のリールだけをランダムに更新
                for (int d = stoppedReels; d < currentStage; d++)
                {
                    result[d] = random.Next(minDigit, maxDigit + 1);
                }
                UpdateDisplayWithCircles(result, displayStep);
                await Task.Delay(spinDelay);
            }

            //黄色予告のときは止まったリールの数字をラッキーナンバーに置き換える
            if (isYellowNotice)
            {
                result[stoppedReels] = forcedLuckyNum[stoppedReels] - '0';
                UpdateDisplayWithCircles(result, displayStep);
            }
            else if (isPremiumNotice)
            {
                result[stoppedReels] = forcedZoromeDigit;
                UpdateDisplayWithCircles(result, displayStep);
            }
        }
        
        circleStep = nextStep;
        
        // 最後の◯（全マル）達成時の決め演出！
        if (isFinalCircleChallenge)
        {
            // 最後の◯を画面に反映！
            UpdateDisplayWithCircles(result, circleStep);
            statusLabel.Text = "🎉 全 マル 達 成 ！！ 🎉";
            statusLabel.TextColor = Colors.Gold;
            
            await Task.Delay(600);
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
        //SUPER RUSH 中の配当処理
        else if (isSuperRush)
        {
            long gainedCoin = (long)numberVal * superRushMultiplier;
            coins += (int)Math.Min(gainedCoin, int.MaxValue);
            statusLabel.Text = $"🔥 RUSH獲得! {numberVal} * {superRushMultiplier}倍 = +{gainedCoin} Coin";
            statusLabel.TextColor = Colors.Gold;
            
            //RUSH中に全マル（①②③）達成で上乗せ！
            if (circleStep == currentStage)
            {
                superRushSpins += 1;
                superRushMultiplier += 10;
                circleStep = 0;
                await PlayZoromeAnimationAsync();
                await DisplayAlertAsync("🎉 上乗せ達成！", "+1G上乗せ & 倍率+10倍アップ！", "NEXT");
            }
            
            if (superRushSpins <= 0)
            {
                isSuperRush = false;
                superRushMultiplier = 10;
                await DisplayAlertAsync("SUPER RUSH 終了", "通常モードに戻ります。", "OK");
            }
        }
        else
        {
            
            if (circleStep == currentStage)
            {
                isSuperRush = true;
                superRushSpins = 1;
                superRushMultiplier = 10;
                circleStep = 0;
                
                await PlayZoromeAnimationAsync();
                await DisplayAlertAsync("⚡ SUPER RUSH 突入！ ⚡", 
                "全マル達成！10スピンの爆発モード突入！\n" +
                "・毎スピンの配当が【10倍】！\n" +
                "・RUSH中に再び全マルで【+1G上乗せ & 倍率+10倍】！", "爆裂スタート！");
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
                    chinchiroPreviousStage = currentStage;
                    minDigit = 1;
                    maxDigit = 6;
                    remainingSpins = 10;
                    isChinChiroMode = true;
                    chinchiroStartCoins = coins;
                    UpdateDisplay(new int[] { 0, 0, 0});

                    await DisplayAlertAsync("チンチロモード突入！",
                    "出目が1〜6限定&10回フリースピン！\n" +
                    "・ピンゾロ(111):5倍配当\n" +
                    "・ゾロ目:3倍配当\n" +
                    "・シゴロ:2倍配当\n" +
                    "・通常の目:賭けコイン返還\n" +
                    "・役無し:賭けたコイン没収\n" +
                    "・ヒフミ:2倍没収\n", "勝負開始！");
                }
                else if (hitDigit == 5)
                {
                    pokerPreviousStage = currentStage;
                    minDigit = 1;
                    maxDigit = 9;
                    currentStage = 5;
                    remainingSpins = 10;
                    isPokerMode = true;
                    pokerStartCoins = coins;
                    UpdateDisplay(new int[] { 0, 0, 0, 0, 0});
                    await DisplayAlertAsync("🃏 ポーカーモード突入！",
                        "出目の数字で役を作る5回限定ポーカー！\n" +
                        "・5カード / 4カード: 超大爆発\n" +
                        "・フルハウス / ストレート / 3カード: 高配当\n" +
                        "・ツーペア / ワンペア: 小役獲得", "勝負開始！");
                }
                else
                {
                    minDigit = 0;
                    maxDigit = 9;
                    isChinChiroMode = false;
                }

                if (hitDigit != 6 && hitDigit != 5)
                {
                    if (currentStage == 3)
                    {
                        await DisplayAlertAsync("突破！", $"{currentProbText}を達成しました" + (maxDigit == 7 ? "\n★7ボーナス:次回は0〜7のみ出現！" : ""), "次のステージへ移行します");
                        currentStage = 4;
                        remainingSpins = 100;
                        UpdateDisplay(new int[] { 0, 0, 0, 0 });
                    }
                    else if (currentStage == 4)
                    {
                        await DisplayAlertAsync("突破！", $"{currentProbText}を達成しました" + (maxDigit == 7 ? "\n★7ボーナス:次回は0〜7のみ出現！" : ""), "次のステージへ移行します");
                        currentStage = 5;
                        remainingSpins = 100;
                        UpdateDisplay(new int[] { 0, 0, 0, 0, 0 });
                    }
                    else
                    {
                        await DisplayAlertAsync("全制覇！", $"{currentProbText}を達成。見事だ。", "次の挑戦を待っている");
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
                await displayLabel.ScaleToAsync(2.0, 300);
                await displayLabel.ScaleToAsync(1.0, 300);

                await Task.Delay(5000);
            }
        }
        //今回の出目を前回の出目として記録
        previousNumber = currentNumber;

        UpdateHeaderUI();
        isSpinning = false;
        spinButton.IsEnabled = true;
        return;
    }

    private void OnScreenTapped(object sender, EventArgs e)
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
            await DisplayAlertAsync("↓Down...↓", "回転数が0になったためステージが1段落ちます", "次へ");
        }
        else if (currentStage == 4)
        {
            currentStage = 3;
            remainingSpins = -1;
            previousNumber = -1;
            maxDigit = 9;
            UpdateDisplay(new int[] { 0, 0, 0 });
            UpdateHeaderUI();
            await DisplayAlertAsync("↓Down...↓", "回転数が0になったため最初のステージに移行します", "Re:TrymenT");

        }
    }

    //リーチ演出
    private async Task PlayReachAnimationAsync()
    {
        reachLabel.IsVisible = true;
        reachLabel.TranslationX = -400;

        //左外から画面中央へスライドイン
        await reachLabel.TranslateToAsync(0, 0, 300, Easing.CubicOut);

        //画面中央へ一時停止
        await Task.Delay(500);

        //右外へスライドアウト
        await reachLabel.TranslateToAsync(400, 0, 300, Easing.CubicIn);

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
    
    private PokerHand EvaluatePoker(Card[] cards)
    {
        bool isFlush = cards.All(c => c.Suit == cards[0].Suit);
        
        var rankGroups = cards.GroupBy(c => c.Rank)
                              .Select(g => g.Count())
                              .OrderByDescending(c => c)
                              .ToList();
                              
        var sortedRanks = cards.Select(c => c.Rank).OrderBy(r => r).ToArray();
        
        // ストレート判定（連番かチェック）
        bool isNormalStraight = true;
        for (int i = 0; i < sortedRanks.Length - 1; i++)
        {
            if (sortedRanks[i + 1] - sortedRanks[i] != 1)
            {
                isNormalStraight = false;
                break;
            }
        }
        
        bool isRoyalStraightFlush = sortedRanks.SequenceEqual(new[] { 1, 10, 11, 12, 13 });
        bool isStraight = isNormalStraight || isRoyalStraightFlush;
        
        if (isFlush && isRoyalStraightFlush) return PokerHand.RoyalStraightFlush;
        if (isFlush && isStraight) return PokerHand.StraightFlush;
        if (rankGroups[0] == 5) return PokerHand.FiveOfAkind;
        if (rankGroups[0] == 4) return PokerHand.FourOfAkind;
        if (rankGroups[0] == 3 && rankGroups.Count > 1 && rankGroups[1] == 2) return PokerHand.FullHouse;
        if (isFlush) return PokerHand.Flush;
        if (isStraight) return PokerHand.Straight;
        if (rankGroups[0] == 3) return PokerHand.ThreeOfAkind;
        if (rankGroups[0] == 2 && rankGroups.Count > 1 && rankGroups[1] == 2) return PokerHand.TwoPair;
        if (rankGroups[0] == 2) return PokerHand.OnePair;
        
        return PokerHand.NoPair;
    }
    
    private ReachData GetReachEffect(bool isFinalCircle, bool isWinning)
    {
        if (isFinalCircle)
        {
            int r = random.Next(100);
            return (r < 70)
                ? GoldReachList[random.Next(GoldReachList.Count)]
                : RedReachList[random.Next(RedReachList.Count)];
        }
        
        int roll = random.Next(100);
        if (isWinning)
        {
            if (roll < 30) return GoldReachList[random.Next(GoldReachList.Count)];
            if (roll < 90) return RedReachList[random.Next(RedReachList.Count)];
            return GreenReachList[random.Next(GreenReachList.Count)];
        }
        else
        {
            if (roll < 60) return BlueReachList[random.Next(BlueReachList.Count)];
            if (roll < 90) return GreenReachList[random.Next(GreenReachList.Count)];
            return RedReachList[random.Next(RedReachList.Count)];
        }
    }

    private async Task PlayZoromeAnimationAsync()
    {
        Color originalColor = displayLabel.TextColor;

        //数字を大きくする
        await displayLabel.ScaleToAsync(1.6, 200, Easing.CubicOut);

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
            await Content.TranslateToAsync(offsetX, 0, 40);
        }

        //画面位置を中央に戻す
        await Content.TranslateToAsync(0, 0, 50);

        //数字が跳ねながら元の位置に戻る
        await displayLabel.ScaleToAsync(1.0, 400, Easing.BounceOut);

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
        await overlayLabel.TranslateToAsync(1500, 0, 3500, Easing.Linear);

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
    
    private async Task<bool> PlaySpecialCountdownReachAsync(int targetDigit, bool isWin)
    {
        // メイン画面を半透明にして演出へ
        await Content.FadeToAsync(0.3, 300);
        
        miniReachLabel.Text = $"[ {targetDigit} | {targetDigit} | ? ]";
        overlayGrid.Opacity = 1;
        overlayGrid.IsVisible = true;
        
        int nextDigit = (targetDigit == 9) ? 0 : targetDigit + 1;
        
        //9から順に上にポイポイ投げる演出
        for (int num = 9; num > nextDigit; num--)
        {
            countdownLabel.Text = num.ToString();
            countdownLabel.Scale = 1.0;
            countdownLabel.TranslationY = 0;
            countdownLabel.Opacity = 1;
            countdownLabel.TextColor = Colors.White;
            
            await Task.Delay(150);
            // 上に飛んで消える
            await Task.WhenAll(
                countdownLabel.TranslateToAsync(0, -250, 200, Easing.CubicIn),
                countdownLabel.FadeToAsync(0, 200)
            );
        }
        
        //「2（テンパイの隣）」が出現してスローモーション＆溜め！
        countdownLabel.Text = nextDigit.ToString();
        countdownLabel.TranslationY = 0;
        countdownLabel.Opacity = 1;
        countdownLabel.TextColor = Colors.Red;
        
        //2.5秒かけてじわじわ巨大化
        await Task.WhenAll(
            countdownLabel.ScaleToAsync(1.8, 2500, Easing.SinOut),
            countdownLabel.RelRotateToAsync(10, 2500)
        );
        
        //当たり外れの分岐
        if (isWin)
        {
            // ★ 当たり！一瞬で「1」にバチコーン切り替わって虹色演出へ
            countdownLabel.Rotation = 0;
            countdownLabel.Scale = 2.5;
            countdownLabel.Text = targetDigit.ToString();
            countdownLabel.TextColor = Colors.Gold;
            
            await countdownLabel.ScaleToAsync(1.2, 400, Easing.BounceOut);
            await Task.Delay(800);
            
            overlayGrid.IsVisible = false;
            await Content.FadeToAsync(1.0, 300);
            return true;
        }
        else
        {
            // ★ ハズレ… ガックリ暗転
            countdownLabel.TextColor = Colors.Gray;
            await Task.Delay(600);
            
            await overlayGrid.FadeToAsync(0, 500);
            overlayGrid.IsVisible = false;
            
            await Content.FadeToAsync(1.0, 300);
            return false;
        }
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
    
    private string ToCircleString(int digit)
    {
        return digit switch
        {
            0 => "⓪", 1 => "①", 2 => "②", 3 => "③", 4 => "④",
            5 => "⑤", 6 => "⑥", 7 => "⑦", 8 => "⑧", 9 => "⑨",
            _ => digit.ToString()
        };
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
    
    private void UpdatePokerDisplay(Card[] cards)
    {
        displayLabel.Text = "[ " + string.Join(" | ", cards.Select( c => c.ToString())) + " ]";
    }
    
    private void UpdateDisplayWithCircles(int[] digits, int step)
    {
        string[] items = new string[digits.Length];
        for (int i = 0; i < digits.Length; i++)
        {
            //達成しているステップ位置までは丸数字、それ以降は通常の数字
            if (i < step)
            {
                items[i] = ToCircleString(digits[i]);
            }
            else
            {
                items[i] = digits[i].ToString();
            }
        }
        displayLabel.Text = "[ " + string.Join(" | ", items) + " ]";
    }
    
    private Card GetRandomCard()
    {
        int suit = random.Next(0, 4);
        int rank = random.Next(1, 14);
        
        return new Card(suit, rank);
    }
}
