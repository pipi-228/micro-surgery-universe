using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Типы бонусов
        private enum BonusType
        {
            None,
            SlowMotion,    // Замедление времени
            DoublePoints,  // Двойные очки
            Shield,        // Щит (защита от аннигиляции)
            MegaWell,      // Мега-гравитационная яма
            Health         // Восстановление жизни
        }

        // Класс бонуса
        private class Bonus
        {
            public Ellipse Visual { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double VX { get; set; }
            public double VY { get; set; }
            public BonusType Type { get; set; }
            public double LifeTime { get; set; } = 5.0; // Живёт 5 секунд
        }

        // Частицы
        private class Particle
        {
            public Rectangle Visual { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double VX { get; set; }
            public double VY { get; set; }
            public double Mass { get; set; }
            public bool IsAntiparticle { get; set; }
            public Color Color { get; set; }
            public double Radius { get; set; }
            public bool IsBonusParticle { get; set; } = false; // Бонусная частица
        }

        // Гравитационная яма
        private class GravityWell
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Strength { get; set; }
            public double Radius { get; set; }
            public Ellipse Visual { get; set; }
            public double LifeTime { get; set; }
        }

        private List<Particle> particles = new List<Particle>();
        private List<GravityWell> gravityWells = new List<GravityWell>();
        private List<Bonus> bonuses = new List<Bonus>();

        private System.Windows.Threading.DispatcherTimer gameTimer;
        private System.Windows.Threading.DispatcherTimer spawnTimer;
        private System.Windows.Threading.DispatcherTimer bonusTimer;

        private Random random = new Random();
        private int score = 0;
        private int lives = 3;
        private int combo = 0;
        private int comboCount = 0;

        private double canvasWidth = 800;
        private double canvasHeight = 600;

        // Активные бонусы
        private BonusType activeBonus = BonusType.None;
        private double activeBonusTimeLeft = 0;
        private bool shieldActive = false;
        private double scoreMultiplier = 1.0;

        // Базовые значения параметров
        private const double BaseGravity = 50;
        private const double BaseMaxSpeed = 300;
        private const double BaseInitialVelocity = 100;
        private const double BaseSpawnMinInterval = 1.5;
        private const double BaseSpawnMaxInterval = 3.0;
        private const int BaseMaxParticles = 50;

        // Текущие параметры
        private double currentGravity;
        private double currentMaxSpeed;
        private double currentInitialVelocity;
        private double currentSpawnMinInterval;
        private double currentSpawnMaxInterval;
        private int currentMaxParticles;

        private double speedMultiplier = 1.0;

        private const double ParticleBaseRadius = 8;

        private readonly Color[] particleColors = new Color[]
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Orange,
            Colors.Violet, Colors.Cyan, Colors.Magenta, Colors.Yellow
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            SpeedSlider.Value = 50;
            UpdateSpeedMultiplier();
        }

        private void InitializeGame()
        {
            GameCanvas.SizeChanged += (s, e) =>
            {
                canvasWidth = GameCanvas.ActualWidth;
                canvasHeight = GameCanvas.ActualHeight;
            };

            gameTimer = new System.Windows.Threading.DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            spawnTimer = new System.Windows.Threading.DispatcherTimer();
            spawnTimer.Tick += SpawnTimer_Tick;
            UpdateSpawnTimer();
            spawnTimer.Start();

            bonusTimer = new System.Windows.Threading.DispatcherTimer();
            bonusTimer.Interval = TimeSpan.FromSeconds(0.1);
            bonusTimer.Tick += BonusTimer_Tick;
            bonusTimer.Start();

            for (int i = 0; i < 15; i++)
            {
                SpawnRandomParticle();
            }
        }

        private void BonusTimer_Tick(object sender, EventArgs e)
        {
            if (activeBonus != BonusType.None)
            {
                activeBonusTimeLeft -= 0.1;
                BonusTimerText.Text = activeBonusTimeLeft.ToString("F1");

                if (activeBonusTimeLeft <= 0)
                {
                    DeactivateBonus();
                }
            }

            // Обновляем отображение активного бонуса
            UpdateBonusDisplay();
        }

        private void ActivateBonus(BonusType type)
        {
            // Если уже активен какой-то бонус, сбрасываем его
            if (activeBonus != BonusType.None)
            {
                DeactivateBonus();
            }

            activeBonus = type;
            activeBonusTimeLeft = 7.0; // Бонус действует 7 секунд

            switch (type)
            {
                case BonusType.SlowMotion:
                    speedMultiplier = 0.4; // Замедление на 60%
                    UpdateSpeedMultiplier();
                    BonusText.Text = "🐢 ЗАМЕДЛЕНИЕ";
                    CreateBonusEffect("🐢 ЗАМЕДЛЕНИЕ", Colors.Cyan);
                    break;

                case BonusType.DoublePoints:
                    scoreMultiplier = 2.0;
                    BonusText.Text = "⭐ ДВОЙНЫЕ ОЧКИ";
                    CreateBonusEffect("⭐ ДВОЙНЫЕ ОЧКИ x2", Colors.Gold);
                    break;

                case BonusType.Shield:
                    shieldActive = true;
                    BonusText.Text = "🛡️ ЩИТ АКТИВЕН";
                    CreateBonusEffect("🛡️ ЩИТ АКТИВЕН", Colors.LightBlue);
                    break;

                case BonusType.MegaWell:
                    // Создаём мощную гравитационную яму в центре
                    CreateMegaGravityWell();
                    BonusText.Text = "🌀 МЕГА-ЯМА";
                    CreateBonusEffect("🌀 МЕГА-ЯМА", Colors.Purple);
                    activeBonusTimeLeft = 0; // Мгновенный бонус
                    DeactivateBonus();
                    break;

                case BonusType.Health:
                    lives = Math.Min(lives + 1, 5);
                    UpdateUI();
                    BonusText.Text = "💚 +1 ЖИЗНЬ";
                    CreateBonusEffect("💚 +1 ЖИЗНЬ", Colors.LimeGreen);
                    activeBonusTimeLeft = 0;
                    DeactivateBonus();
                    break;
            }

            UpdateBonusDisplay();
        }

        private void DeactivateBonus()
        {
            switch (activeBonus)
            {
                case BonusType.SlowMotion:
                    speedMultiplier = SpeedSlider.Value / 100 * 1.7 + 0.3;
                    UpdateSpeedMultiplier();
                    break;
                case BonusType.DoublePoints:
                    scoreMultiplier = 1.0;
                    break;
                case BonusType.Shield:
                    shieldActive = false;
                    break;
            }

            activeBonus = BonusType.None;
            activeBonusTimeLeft = 0;
            BonusTimerText.Text = "0.0";
            UpdateBonusDisplay();
        }

        private void UpdateBonusDisplay()
        {
            if (activeBonus == BonusType.None)
            {
                BonusText.Text = "АКТИВЕН: НЕТ";
                BonusText.Foreground = new SolidColorBrush(Colors.Gray);
            }
            else
            {
                BonusText.Foreground = new SolidColorBrush(Colors.Gold);
            }
        }

        private void CreateBonusEffect(string text, Color color)
        {
            TextBlock effectText = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Opacity = 1,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Color = color
                }
            };

            Canvas.SetLeft(effectText, canvasWidth / 2 - 100);
            Canvas.SetTop(effectText, canvasHeight / 2 - 50);
            GameCanvas.Children.Add(effectText);

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.5)
            };
            DoubleAnimation moveUp = new DoubleAnimation
            {
                From = canvasHeight / 2 - 50,
                To = canvasHeight / 2 - 150,
                Duration = TimeSpan.FromSeconds(1.5)
            };

            effectText.BeginAnimation(TextBlock.OpacityProperty, fadeOut);
            effectText.BeginAnimation(Canvas.TopProperty, moveUp);

            System.Windows.Threading.DispatcherTimer cleanup = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            cleanup.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(effectText);
                cleanup.Stop();
            };
            cleanup.Start();
        }

        private void CreateMegaGravityWell()
        {
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            GravityWell megaWell = new GravityWell
            {
                X = centerX,
                Y = centerY,
                Strength = 8000,
                Radius = 200,
                LifeTime = 4.0,
                Visual = new Ellipse
                {
                    Width = 400,
                    Height = 400,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 128, 0, 128)),
                    Stroke = new SolidColorBrush(Color.FromArgb(200, 128, 0, 128)),
                    StrokeThickness = 4,
                    StrokeDashArray = new DoubleCollection { 10, 5 },
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 30,
                        ShadowDepth = 0,
                        Color = Colors.Purple
                    }
                }
            };

            gravityWells.Add(megaWell);
            Canvas.SetLeft(megaWell.Visual, megaWell.X - megaWell.Radius);
            Canvas.SetTop(megaWell.Visual, megaWell.Y - megaWell.Radius);
            GameCanvas.Children.Add(megaWell.Visual);

            // Эффект появления мега-ямы
            for (int i = 0; i < 30; i++)
            {
                Ellipse particle = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = new SolidColorBrush(Colors.Purple),
                    Opacity = 0.8
                };
                double angle = random.NextDouble() * Math.PI * 2;
                double dist = random.NextDouble() * 150;
                Canvas.SetLeft(particle, centerX + Math.Cos(angle) * dist);
                Canvas.SetTop(particle, centerY + Math.Sin(angle) * dist);
                GameCanvas.Children.Add(particle);

                System.Windows.Threading.DispatcherTimer moveTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                double elapsed = 0;
                double duration = 0.8;
                double fromX = centerX + Math.Cos(angle) * dist;
                double fromY = centerY + Math.Sin(angle) * dist;

                moveTimer.Tick += (s, args) =>
                {
                    elapsed += 0.016;
                    double t = elapsed / duration;
                    if (t >= 1)
                    {
                        moveTimer.Stop();
                        GameCanvas.Children.Remove(particle);
                    }
                    else
                    {
                        double newX = fromX + (centerX - fromX) * t;
                        double newY = fromY + (centerY - fromY) * t;
                        Canvas.SetLeft(particle, newX);
                        Canvas.SetTop(particle, newY);
                        particle.Opacity = 1 - t;
                    }
                };
                moveTimer.Start();
            }
        }

        private void SpawnBonus(double x, double y)
        {
            BonusType type = (BonusType)random.Next(1, 6); // 1-5 типы бонусов
            Color bonusColor;
            string bonusSymbol;

            switch (type)
            {
                case BonusType.SlowMotion:
                    bonusColor = Colors.Cyan;
                    bonusSymbol = "🐢";
                    break;
                case BonusType.DoublePoints:
                    bonusColor = Colors.Gold;
                    bonusSymbol = "⭐";
                    break;
                case BonusType.Shield:
                    bonusColor = Colors.LightBlue;
                    bonusSymbol = "🛡️";
                    break;
                case BonusType.MegaWell:
                    bonusColor = Colors.Purple;
                    bonusSymbol = "🌀";
                    break;
                case BonusType.Health:
                    bonusColor = Colors.LimeGreen;
                    bonusSymbol = "💚";
                    break;
                default:
                    bonusColor = Colors.White;
                    bonusSymbol = "?";
                    break;
            }

            Bonus bonus = new Bonus
            {
                X = x,
                Y = y,
                VX = (random.NextDouble() - 0.5) * 50,
                VY = (random.NextDouble() - 0.5) * 50 - 30,
                Type = type,
                Visual = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(bonusColor),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 10,
                        ShadowDepth = 0,
                        Color = bonusColor
                    }
                }
            };

            // Добавляем текст бонуса
            TextBlock bonusText = new TextBlock
            {
                Text = bonusSymbol,
                FontSize = 12,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };

            // Сохраняем текст в Tag бонуса (для обновления позиции)
            bonus.Visual.Tag = bonusText;

            bonuses.Add(bonus);
            GameCanvas.Children.Add(bonus.Visual);
            GameCanvas.Children.Add(bonusText);

            UpdateBonusPosition(bonus);
        }

        private void UpdateBonusPosition(Bonus bonus)
        {
            Canvas.SetLeft(bonus.Visual, bonus.X - 10);
            Canvas.SetTop(bonus.Visual, bonus.Y - 10);

            if (bonus.Visual.Tag is TextBlock text)
            {
                Canvas.SetLeft(text, bonus.X - 8);
                Canvas.SetTop(text, bonus.Y - 8);
            }
        }

        private void CheckBonusCollection()
        {
            List<Bonus> collectedBonuses = new List<Bonus>();

            foreach (var bonus in bonuses)
            {
                // Проверяем клик по бонусу (ЛКМ)
                Point mousePos = Mouse.GetPosition(GameCanvas);
                double dx = bonus.X - mousePos.X;
                double dy = bonus.Y - mousePos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 15)
                {
                    collectedBonuses.Add(bonus);
                    ActivateBonus(bonus.Type);

                    // Эффект сбора бонуса
                    CreateBonusCollectEffect(bonus.X, bonus.Y);
                }
            }

            foreach (var bonus in collectedBonuses)
            {
                bonuses.Remove(bonus);
                GameCanvas.Children.Remove(bonus.Visual);
                if (bonus.Visual.Tag is TextBlock text)
                {
                    GameCanvas.Children.Remove(text);
                }
            }
        }

        private void CreateBonusCollectEffect(double x, double y)
        {
            for (int i = 0; i < 15; i++)
            {
                Ellipse spark = new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Fill = new SolidColorBrush(Colors.Gold),
                    Opacity = 0.9
                };
                Canvas.SetLeft(spark, x);
                Canvas.SetTop(spark, y);
                GameCanvas.Children.Add(spark);

                double angle = random.NextDouble() * Math.PI * 2;
                double speed = random.NextDouble() * 100 + 50;
                double startX = x;
                double startY = y;
                double vx = Math.Cos(angle) * speed;
                double vy = Math.Sin(angle) * speed;

                System.Windows.Threading.DispatcherTimer moveTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                double elapsed = 0;
                double duration = 0.5;

                moveTimer.Tick += (s, args) =>
                {
                    elapsed += 0.016;
                    double t = elapsed / duration;
                    if (t >= 1)
                    {
                        moveTimer.Stop();
                        GameCanvas.Children.Remove(spark);
                    }
                    else
                    {
                        double newX = startX + vx * t;
                        double newY = startY + vy * t;
                        Canvas.SetLeft(spark, newX);
                        Canvas.SetTop(spark, newY);
                        spark.Opacity = 1 - t;
                    }
                };
                moveTimer.Start();
            }
        }

        private void UpdateSpeedMultiplier()
        {
            double sliderValue = SpeedSlider.Value;
            speedMultiplier = (sliderValue / 100.0) * 1.7 + 0.3;

            SpeedValueText.Text = ((int)(speedMultiplier * 100)).ToString();

            if (speedMultiplier < 0.7)
                SpeedValueText.Foreground = new SolidColorBrush(Colors.LightGreen);
            else if (speedMultiplier > 1.3)
                SpeedValueText.Foreground = new SolidColorBrush(Colors.Orange);
            else
                SpeedValueText.Foreground = new SolidColorBrush(Colors.White);

            currentGravity = BaseGravity * speedMultiplier;
            currentMaxSpeed = BaseMaxSpeed * speedMultiplier;
            currentInitialVelocity = BaseInitialVelocity * speedMultiplier;
            currentSpawnMinInterval = BaseSpawnMinInterval / speedMultiplier;
            currentSpawnMaxInterval = BaseSpawnMaxInterval / speedMultiplier;
            currentMaxParticles = (int)(BaseMaxParticles * (0.7 + speedMultiplier * 0.5));

            foreach (var particle in particles)
            {
                double factor = speedMultiplier / 1.0;
                particle.VX = particle.VX * factor;
                particle.VY = particle.VY * factor;

                double newSpeed = Math.Sqrt(particle.VX * particle.VX + particle.VY * particle.VY);
                if (newSpeed > currentMaxSpeed)
                {
                    particle.VX = particle.VX / newSpeed * currentMaxSpeed;
                    particle.VY = particle.VY / newSpeed * currentMaxSpeed;
                }
            }

            UpdateSpawnTimer();
        }

        private void UpdateSpawnTimer()
        {
            double interval = random.NextDouble() * (currentSpawnMaxInterval - currentSpawnMinInterval) + currentSpawnMinInterval;
            spawnTimer.Interval = TimeSpan.FromSeconds(Math.Max(0.3, interval));
        }

        private void SpawnRandomParticle()
        {
            double radius = ParticleBaseRadius + random.NextDouble() * 6;
            bool isAnti = random.Next(0, 2) == 1;
            Color baseColor = particleColors[random.Next(particleColors.Length)];

            // 5% шанс создать бонусную частицу
            bool isBonusParticle = random.Next(0, 100) < 5;
            Color particleColor = isBonusParticle ? Colors.Gold : (isAnti ?
                Color.FromArgb(255, (byte)(baseColor.R / 2), (byte)(baseColor.G / 2), (byte)(baseColor.B / 2)) :
                baseColor);

            Particle particle = new Particle
            {
                X = random.NextDouble() * (canvasWidth - 100) + 50,
                Y = random.NextDouble() * (canvasHeight - 100) + 50,
                VX = (random.NextDouble() - 0.5) * currentInitialVelocity,
                VY = (random.NextDouble() - 0.5) * currentInitialVelocity,
                Mass = radius,
                IsAntiparticle = isAnti,
                Color = baseColor,
                Radius = radius,
                IsBonusParticle = isBonusParticle,
                Visual = new Rectangle
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = new SolidColorBrush(particleColor),
                    Stroke = isBonusParticle ? new SolidColorBrush(Colors.Gold) : (isAnti ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.White)),
                    StrokeThickness = isBonusParticle ? 3 : 2,
                    RadiusX = radius,
                    RadiusY = radius,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = isBonusParticle ? 20 : 15,
                        ShadowDepth = 0,
                        Opacity = 0.7,
                        Color = particleColor
                    }
                }
            };

            particles.Add(particle);
            GameCanvas.Children.Add(particle.Visual);
            UpdateParticlePosition(particle);
        }

        private void SpawnTimer_Tick(object sender, EventArgs e)
        {
            if (lives > 0 && particles.Count < currentMaxParticles)
            {
                int spawnCount = random.Next(1, 3);
                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnRandomParticle();
                }
                UpdateSpawnTimer();
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (lives <= 0) return;

            UpdatePhysics();
            UpdateGravityWells();
            UpdateBonuses();
            UpdateUI();
            CheckCollisionsAndAnnihilation();
            CheckBonusCollection();
        }

        private void UpdateBonuses()
        {
            List<Bonus> toRemove = new List<Bonus>();

            foreach (var bonus in bonuses)
            {
                // Движение бонуса
                bonus.X += bonus.VX * gameTimer.Interval.TotalSeconds;
                bonus.Y += bonus.VY * gameTimer.Interval.TotalSeconds;
                bonus.VY += 100 * gameTimer.Interval.TotalSeconds; // Гравитация для бонусов

                // Границы
                if (bonus.X - 10 < 0 || bonus.X + 10 > canvasWidth)
                    bonus.VX = -bonus.VX;
                if (bonus.Y - 10 < 0 || bonus.Y + 10 > canvasHeight)
                    bonus.VY = -bonus.VY;

                bonus.X = Math.Max(10, Math.Min(canvasWidth - 10, bonus.X));
                bonus.Y = Math.Max(10, Math.Min(canvasHeight - 10, bonus.Y));

                // Уменьшаем время жизни
                bonus.LifeTime -= gameTimer.Interval.TotalSeconds;
                if (bonus.LifeTime <= 0)
                {
                    toRemove.Add(bonus);
                }

                UpdateBonusPosition(bonus);
            }

            foreach (var bonus in toRemove)
            {
                bonuses.Remove(bonus);
                GameCanvas.Children.Remove(bonus.Visual);
                if (bonus.Visual.Tag is TextBlock text)
                {
                    GameCanvas.Children.Remove(text);
                }
            }
        }

        private void UpdatePhysics()
        {
            double[] ax = new double[particles.Count];
            double[] ay = new double[particles.Count];

            for (int i = 0; i < particles.Count; i++)
            {
                ax[i] = 0;
                ay[i] = 0;

                for (int j = i + 1; j < particles.Count; j++)
                {
                    double dx = particles[j].X - particles[i].X;
                    double dy = particles[j].Y - particles[i].Y;
                    double distSq = dx * dx + dy * dy;
                    double dist = Math.Sqrt(distSq);

                    if (dist < 5) continue;

                    double forceMagnitude = currentGravity * particles[i].Mass * particles[j].Mass / distSq;

                    bool sameColor = (particles[i].Color.R == particles[j].Color.R &&
                                      particles[i].Color.G == particles[j].Color.G &&
                                      particles[i].Color.B == particles[j].Color.B);
                    bool sameTypeColor = (particles[i].IsAntiparticle == particles[j].IsAntiparticle) && sameColor;

                    if (!sameTypeColor)
                    {
                        forceMagnitude = -forceMagnitude;
                    }

                    double fx = forceMagnitude * dx / dist;
                    double fy = forceMagnitude * dy / dist;

                    ax[i] += fx / particles[i].Mass;
                    ay[i] += fy / particles[i].Mass;
                    ax[j] -= fx / particles[j].Mass;
                    ay[j] -= fy / particles[j].Mass;
                }

                foreach (var well in gravityWells)
                {
                    double dx = well.X - particles[i].X;
                    double dy = well.Y - particles[i].Y;
                    double distSq = dx * dx + dy * dy;
                    double dist = Math.Sqrt(distSq);

                    if (dist < well.Radius && dist > 5)
                    {
                        double forceMagnitude = well.Strength * 30000 * particles[i].Mass / distSq;
                        double fx = forceMagnitude * dx / dist;
                        double fy = forceMagnitude * dy / dist;
                        ax[i] += fx / particles[i].Mass;
                        ay[i] += fy / particles[i].Mass;
                    }
                }
            }

            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].VX += ax[i] * gameTimer.Interval.TotalSeconds;
                particles[i].VY += ay[i] * gameTimer.Interval.TotalSeconds;

                double speed = Math.Sqrt(particles[i].VX * particles[i].VX + particles[i].VY * particles[i].VY);
                if (speed > currentMaxSpeed)
                {
                    particles[i].VX = particles[i].VX / speed * currentMaxSpeed;
                    particles[i].VY = particles[i].VY / speed * currentMaxSpeed;
                }

                particles[i].X += particles[i].VX * gameTimer.Interval.TotalSeconds;
                particles[i].Y += particles[i].VY * gameTimer.Interval.TotalSeconds;

                if (particles[i].X - particles[i].Radius < 0)
                {
                    particles[i].X = particles[i].Radius;
                    particles[i].VX = -particles[i].VX * 0.8;
                }
                if (particles[i].X + particles[i].Radius > canvasWidth)
                {
                    particles[i].X = canvasWidth - particles[i].Radius;
                    particles[i].VX = -particles[i].VX * 0.8;
                }
                if (particles[i].Y - particles[i].Radius < 0)
                {
                    particles[i].Y = particles[i].Radius;
                    particles[i].VY = -particles[i].VY * 0.8;
                }
                if (particles[i].Y + particles[i].Radius > canvasHeight)
                {
                    particles[i].Y = canvasHeight - particles[i].Radius;
                    particles[i].VY = -particles[i].VY * 0.8;
                }

                UpdateParticlePosition(particles[i]);
            }
        }

        private void UpdateGravityWells()
        {
            List<GravityWell> toRemove = new List<GravityWell>();

            foreach (var well in gravityWells)
            {
                well.LifeTime -= gameTimer.Interval.TotalSeconds;

                if (well.LifeTime <= 0)
                {
                    toRemove.Add(well);
                }
                else
                {
                    double opacity = Math.Min(1, well.LifeTime / 1.5) * 0.6;
                    well.Visual.Opacity = opacity;
                    well.Visual.Width = well.Radius * 2 * (0.8 + (1 - well.LifeTime / 3) * 0.4);
                    well.Visual.Height = well.Radius * 2 * (0.8 + (1 - well.LifeTime / 3) * 0.4);

                    if (well.LifeTime < 1)
                    {
                        well.Visual.Stroke = new SolidColorBrush(Colors.Red);
                    }
                }
            }

            foreach (var well in toRemove)
            {
                gravityWells.Remove(well);
                GameCanvas.Children.Remove(well.Visual);
            }
        }

        private void CheckCollisionsAndAnnihilation()
        {
            List<Particle> toRemove = new List<Particle>();

            for (int i = 0; i < particles.Count; i++)
            {
                for (int j = i + 1; j < particles.Count; j++)
                {
                    double dx = particles[i].X - particles[j].X;
                    double dy = particles[i].Y - particles[j].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    double minDist = particles[i].Radius + particles[j].Radius;

                    if (dist < minDist)
                    {
                        bool sameColor = (particles[i].Color.R == particles[j].Color.R &&
                                          particles[i].Color.G == particles[j].Color.G &&
                                          particles[i].Color.B == particles[j].Color.B);

                        bool isAnnihilation = (particles[i].IsAntiparticle != particles[j].IsAntiparticle) && sameColor;

                        if (isAnnihilation)
                        {
                            toRemove.Add(particles[i]);
                            toRemove.Add(particles[j]);

                            // ЩИТ защищает от потери жизни
                            if (!shieldActive)
                            {
                                lives--;
                                combo = 0;
                                comboCount = 0;
                            }
                            else
                            {
                                CreateShieldEffect(particles[i].X, particles[i].Y);
                            }

                            CreateImprovedAnnihilationEffect(particles[i].X, particles[i].Y, particles[i].Color);

                            if (lives <= 0)
                            {
                                GameOver();
                            }
                            break;
                        }
                        else
                        {
                            ResolveCollision(particles[i], particles[j]);
                        }
                    }
                }
            }

            foreach (var particle in toRemove)
            {
                particles.Remove(particle);
                GameCanvas.Children.Remove(particle.Visual);
            }

            UpdateParticlesCount();
            UpdateUI();

            if (particles.Count > currentMaxParticles && lives > 0)
            {
                lives--;
                if (lives <= 0) GameOver();
                UpdateUI();
                CreateWarningEffect();
            }
        }

        private void CreateShieldEffect(double x, double y)
        {
            Ellipse shield = new Ellipse
            {
                Width = 30,
                Height = 30,
                Stroke = new SolidColorBrush(Colors.Cyan),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(80, 0, 255, 255)),
                Opacity = 0.8
            };
            Canvas.SetLeft(shield, x - 15);
            Canvas.SetTop(shield, y - 15);
            GameCanvas.Children.Add(shield);

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 0.8,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            shield.BeginAnimation(Ellipse.OpacityProperty, fadeOut);

            System.Windows.Threading.DispatcherTimer cleanup = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            cleanup.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(shield);
                cleanup.Stop();
            };
            cleanup.Start();
        }

        private void CreateImprovedAnnihilationEffect(double x, double y, Color color)
        {
            Ellipse flash = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(color),
                Opacity = 1
            };
            Canvas.SetLeft(flash, x - 15);
            Canvas.SetTop(flash, y - 15);
            GameCanvas.Children.Add(flash);

            DoubleAnimation flashOpacity = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            flash.BeginAnimation(Ellipse.OpacityProperty, flashOpacity);

            DoubleAnimation flashScaleX = new DoubleAnimation
            {
                From = 1,
                To = 5,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            DoubleAnimation flashScaleY = new DoubleAnimation
            {
                From = 1,
                To = 5,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            ScaleTransform flashScale = new ScaleTransform();
            flash.RenderTransform = flashScale;
            flashScale.BeginAnimation(ScaleTransform.ScaleXProperty, flashScaleX);
            flashScale.BeginAnimation(ScaleTransform.ScaleYProperty, flashScaleY);

            System.Windows.Threading.DispatcherTimer cleanupFlash = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.3)
            };
            cleanupFlash.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(flash);
                cleanupFlash.Stop();
            };
            cleanupFlash.Start();

            Random rand = new Random();
            for (int i = 0; i < 30; i++)
            {
                Ellipse shard = new Ellipse
                {
                    Width = 4 + rand.NextDouble() * 6,
                    Height = 4 + rand.NextDouble() * 6,
                    Fill = new SolidColorBrush(color),
                    Opacity = 0.9
                };

                Canvas.SetLeft(shard, x - 3);
                Canvas.SetTop(shard, y - 3);
                GameCanvas.Children.Add(shard);

                double angle = rand.NextDouble() * Math.PI * 2;
                double speed = rand.NextDouble() * 200 + 50;
                double vx = Math.Cos(angle) * speed;
                double vy = Math.Sin(angle) * speed;

                double startX = x;
                double startY = y;

                System.Windows.Threading.DispatcherTimer moveTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                double elapsed = 0;
                double duration = 0.8;

                moveTimer.Tick += (s, args) =>
                {
                    elapsed += 0.016;
                    double t = elapsed / duration;
                    if (t >= 1)
                    {
                        moveTimer.Stop();
                        GameCanvas.Children.Remove(shard);
                    }
                    else
                    {
                        double newX = startX + vx * t;
                        double newY = startY + vy * t;
                        Canvas.SetLeft(shard, newX);
                        Canvas.SetTop(shard, newY);
                        shard.Opacity = 1 - t;
                        shard.RenderTransform = new RotateTransform(angle * 180 / Math.PI + t * 360);
                    }
                };
                moveTimer.Start();
            }

            for (int ring = 0; ring < 3; ring++)
            {
                Ellipse shockwave = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 3,
                    Opacity = 0.8
                };
                Canvas.SetLeft(shockwave, x - 5);
                Canvas.SetTop(shockwave, y - 5);
                GameCanvas.Children.Add(shockwave);

                DoubleAnimation waveScaleX = new DoubleAnimation
                {
                    From = 1,
                    To = 20,
                    Duration = TimeSpan.FromSeconds(0.6),
                    BeginTime = TimeSpan.FromSeconds(ring * 0.1)
                };
                DoubleAnimation waveScaleY = new DoubleAnimation
                {
                    From = 1,
                    To = 20,
                    Duration = TimeSpan.FromSeconds(0.6),
                    BeginTime = TimeSpan.FromSeconds(ring * 0.1)
                };
                DoubleAnimation waveOpacity = new DoubleAnimation
                {
                    From = 0.8,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.6),
                    BeginTime = TimeSpan.FromSeconds(ring * 0.1)
                };

                ScaleTransform waveScale = new ScaleTransform();
                shockwave.RenderTransform = waveScale;

                waveScale.BeginAnimation(ScaleTransform.ScaleXProperty, waveScaleX);
                waveScale.BeginAnimation(ScaleTransform.ScaleYProperty, waveScaleY);
                shockwave.BeginAnimation(Ellipse.OpacityProperty, waveOpacity);

                System.Windows.Threading.DispatcherTimer cleanupWave = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.7 + ring * 0.1)
                };
                cleanupWave.Tick += (s, args) =>
                {
                    GameCanvas.Children.Remove(shockwave);
                    cleanupWave.Stop();
                };
                cleanupWave.Start();
            }

            TextBlock annText = new TextBlock
            {
                Text = "💥 АННИГИЛЯЦИЯ! 💥",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Opacity = 1,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Color = color
                }
            };
            Canvas.SetLeft(annText, x - 80);
            Canvas.SetTop(annText, y - 30);
            GameCanvas.Children.Add(annText);

            DoubleAnimation textFade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1)
            };
            DoubleAnimation textMove = new DoubleAnimation
            {
                From = y - 30,
                To = y - 80,
                Duration = TimeSpan.FromSeconds(1)
            };
            annText.BeginAnimation(TextBlock.OpacityProperty, textFade);
            annText.BeginAnimation(Canvas.TopProperty, textMove);

            System.Windows.Threading.DispatcherTimer cleanupText = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            cleanupText.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(annText);
                cleanupText.Stop();
            };
            cleanupText.Start();

            DoubleAnimation shakeX = new DoubleAnimation
            {
                From = -5,
                To = 5,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            DoubleAnimation shakeY = new DoubleAnimation
            {
                From = -3,
                To = 3,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            GameCanvas.BeginAnimation(Canvas.LeftProperty, shakeX);
            GameCanvas.BeginAnimation(Canvas.TopProperty, shakeY);

            System.Windows.Threading.DispatcherTimer stopShake = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            stopShake.Tick += (s, args) =>
            {
                GameCanvas.BeginAnimation(Canvas.LeftProperty, null);
                GameCanvas.BeginAnimation(Canvas.TopProperty, null);
                stopShake.Stop();
            };
            stopShake.Start();
        }

        private void CreateWarningEffect()
        {
            Border warningBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(150, 255, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 0
            };

            GameCanvas.Children.Add(warningBorder);
            Canvas.SetLeft(warningBorder, 0);
            Canvas.SetTop(warningBorder, 0);
            warningBorder.Width = canvasWidth;
            warningBorder.Height = canvasHeight;

            DoubleAnimation flashWarning = new DoubleAnimation
            {
                From = 0,
                To = 0.5,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            warningBorder.BeginAnimation(Border.OpacityProperty, flashWarning);

            System.Windows.Threading.DispatcherTimer cleanupWarning = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            cleanupWarning.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(warningBorder);
                cleanupWarning.Stop();
            };
            cleanupWarning.Start();
        }

        private void ResolveCollision(Particle p1, Particle p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double minDist = p1.Radius + p2.Radius;

            double overlap = minDist - dist;
            double nx = dx / dist;
            double ny = dy / dist;

            p1.X -= nx * overlap * 0.5;
            p1.Y -= ny * overlap * 0.5;
            p2.X += nx * overlap * 0.5;
            p2.Y += ny * overlap * 0.5;

            double rvx = p2.VX - p1.VX;
            double rvy = p2.VY - p1.VY;
            double velAlong = rvx * nx + rvy * ny;

            if (velAlong > 0) return;

            double restitution = 0.7;
            double im1 = 1.0 / p1.Mass;
            double im2 = 1.0 / p2.Mass;
            double impulse = (1 + restitution) * velAlong / (im1 + im2);

            p1.VX += impulse * im1 * nx;
            p1.VY += impulse * im1 * ny;
            p2.VX -= impulse * im2 * nx;
            p2.VY -= impulse * im2 * ny;
        }

        private void UpdateParticlePosition(Particle p)
        {
            Canvas.SetLeft(p.Visual, p.X - p.Radius);
            Canvas.SetTop(p.Visual, p.Y - p.Radius);
        }

        private void UpdateUI()
        {
            ScoreText.Text = score.ToString();
            LivesText.Text = lives.ToString();

            if (lives <= 0)
            {
                LivesText.Foreground = new SolidColorBrush(Colors.Gray);
            }
            else if (lives == 1)
            {
                LivesText.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                LivesText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
        }

        private void UpdateParticlesCount()
        {
            ParticlesText.Text = particles.Count.ToString();

            if (particles.Count > currentMaxParticles - 10)
            {
                ParticlesText.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else if (particles.Count > currentMaxParticles - 20)
            {
                ParticlesText.Foreground = new SolidColorBrush(Colors.Yellow);
            }
            else
            {
                ParticlesText.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lives <= 0) return;

            Point clickPos = e.GetPosition(GameCanvas);

            // Сначала проверяем бонусы
            Bonus clickedBonus = null;
            foreach (var bonus in bonuses)
            {
                double dx = clickPos.X - bonus.X;
                double dy = clickPos.Y - bonus.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 15)
                {
                    clickedBonus = bonus;
                    break;
                }
            }

            if (clickedBonus != null)
            {
                bonuses.Remove(clickedBonus);
                GameCanvas.Children.Remove(clickedBonus.Visual);
                if (clickedBonus.Visual.Tag is TextBlock text)
                {
                    GameCanvas.Children.Remove(text);
                }
                ActivateBonus(clickedBonus.Type);
                CreateBonusCollectEffect(clickedBonus.X, clickedBonus.Y);
                return;
            }

            // Затем проверяем частицы
            Particle clickedParticle = null;
            foreach (var p in particles)
            {
                double dx = clickPos.X - p.X;
                double dy = clickPos.Y - p.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist <= p.Radius)
                {
                    clickedParticle = p;
                    break;
                }
            }

            if (clickedParticle != null)
            {
                particles.Remove(clickedParticle);
                GameCanvas.Children.Remove(clickedParticle.Visual);

                int pointsGained = clickedParticle.IsBonusParticle ? 50 : 10;
                score += (int)(pointsGained * scoreMultiplier);

                // Система комбо
                combo++;
                comboCount++;
                if (comboCount >= 5)
                {
                    score += 20;
                    CreateComboEffect(clickedParticle.X, clickedParticle.Y, comboCount);
                }

                CreateDeletionEffect(clickedParticle.X, clickedParticle.Y, pointsGained, scoreMultiplier);

                // Шанс выпадения бонуса при удалении
                if (random.Next(0, 100) < 15) // 15% шанс
                {
                    SpawnBonus(clickedParticle.X, clickedParticle.Y);
                }

                UpdateParticlesCount();
            }
            else
            {
                comboCount = 0;
            }
        }

        private void CreateComboEffect(double x, double y, int comboLevel)
        {
            TextBlock comboText = new TextBlock
            {
                Text = $"🔥 x{comboLevel} COMBO! +20 🔥",
                Foreground = new SolidColorBrush(Colors.OrangeRed),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Opacity = 1
            };
            Canvas.SetLeft(comboText, x - 50);
            Canvas.SetTop(comboText, y - 30);
            GameCanvas.Children.Add(comboText);

            DoubleAnimation moveUp = new DoubleAnimation
            {
                From = y - 30,
                To = y - 80,
                Duration = TimeSpan.FromSeconds(1)
            };
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(1)
            };

            comboText.BeginAnimation(Canvas.TopProperty, moveUp);
            comboText.BeginAnimation(TextBlock.OpacityProperty, fadeOut);

            System.Windows.Threading.DispatcherTimer cleanup = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            cleanup.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(comboText);
                cleanup.Stop();
            };
            cleanup.Start();
        }

        private void CreateDeletionEffect(double x, double y, int points, double multiplier)
        {
            for (int i = 0; i < 8; i++)
            {
                Ellipse spark = new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Fill = new SolidColorBrush(Colors.LimeGreen),
                    Opacity = 0.9
                };
                Canvas.SetLeft(spark, x);
                Canvas.SetTop(spark, y);
                GameCanvas.Children.Add(spark);

                double angle = random.NextDouble() * Math.PI * 2;
                double speed = random.NextDouble() * 100 + 30;

                double startX = x;
                double startY = y;
                double vx = Math.Cos(angle) * speed;
                double vy = Math.Sin(angle) * speed;

                System.Windows.Threading.DispatcherTimer moveTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                double elapsed = 0;
                double duration = 0.5;

                moveTimer.Tick += (s, args) =>
                {
                    elapsed += 0.016;
                    double t = elapsed / duration;
                    if (t >= 1)
                    {
                        moveTimer.Stop();
                        GameCanvas.Children.Remove(spark);
                    }
                    else
                    {
                        double newX = startX + vx * t;
                        double newY = startY + vy * t;
                        Canvas.SetLeft(spark, newX);
                        Canvas.SetTop(spark, newY);
                        spark.Opacity = 1 - t;
                    }
                };
                moveTimer.Start();
            }

            string pointsText = multiplier > 1 ? $"+{points} x{multiplier}!" : $"+{points}";
            TextBlock pointsBlock = new TextBlock
            {
                Text = pointsText,
                Foreground = multiplier > 1 ? new SolidColorBrush(Colors.Gold) : new SolidColorBrush(Colors.LimeGreen),
                FontSize = multiplier > 1 ? 20 : 18,
                FontWeight = FontWeights.Bold,
                Opacity = 1,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 5,
                    ShadowDepth = 0,
                    Color = multiplier > 1 ? Colors.Gold : Colors.LimeGreen
                }
            };

            Canvas.SetLeft(pointsBlock, x);
            Canvas.SetTop(pointsBlock, y);
            GameCanvas.Children.Add(pointsBlock);

            DoubleAnimation moveUp = new DoubleAnimation
            {
                From = y,
                To = y - 40,
                Duration = TimeSpan.FromSeconds(0.8)
            };

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.8)
            };

            Storyboard storyboard = new Storyboard();
            Storyboard.SetTarget(moveUp, pointsBlock);
            Storyboard.SetTargetProperty(moveUp, new PropertyPath("(Canvas.Top)"));
            Storyboard.SetTarget(fadeOut, pointsBlock);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(TextBlock.OpacityProperty));

            storyboard.Children.Add(moveUp);
            storyboard.Children.Add(fadeOut);
            storyboard.Completed += (s, a) => GameCanvas.Children.Remove(pointsBlock);
            storyboard.Begin();
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lives <= 0) return;

            Point clickPos = e.GetPosition(GameCanvas);

            double strength = 3000 + random.Next(-1000, 1000);
            Color wellColor = strength > 0 ? Colors.LightGreen : Colors.Orange;

            CreateGravityWellEffect(clickPos.X, clickPos.Y, strength, wellColor);

            GravityWell well = new GravityWell
            {
                X = clickPos.X,
                Y = clickPos.Y,
                Strength = strength,
                Radius = 120,
                LifeTime = 3.0,
                Visual = new Ellipse
                {
                    Width = 240,
                    Height = 240,
                    Fill = new SolidColorBrush(Color.FromArgb(40, wellColor.R, wellColor.G, wellColor.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb(200, wellColor.R, wellColor.G, wellColor.B)),
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection { 8, 4 },
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Color = wellColor
                    }
                }
            };

            gravityWells.Add(well);
            Canvas.SetLeft(well.Visual, well.X - well.Radius);
            Canvas.SetTop(well.Visual, well.Y - well.Radius);
            GameCanvas.Children.Add(well.Visual);

            TextBlock wellLabel = new TextBlock
            {
                Text = strength > 0 ? "🌀 ПРИТЯЖЕНИЕ +" : "💨 ОТТАЛКИВАНИЕ -",
                Foreground = new SolidColorBrush(wellColor),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Opacity = 0.9,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 5,
                    ShadowDepth = 0,
                    Color = Colors.Black
                }
            };
            Canvas.SetLeft(wellLabel, clickPos.X - 60);
            Canvas.SetTop(wellLabel, clickPos.Y - 40);
            GameCanvas.Children.Add(wellLabel);

            DoubleAnimation labelFade = new DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.5),
                BeginTime = TimeSpan.FromSeconds(0.5)
            };
            wellLabel.BeginAnimation(TextBlock.OpacityProperty, labelFade);

            System.Windows.Threading.DispatcherTimer cleanupLabel = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            cleanupLabel.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(wellLabel);
                cleanupLabel.Stop();
            };
            cleanupLabel.Start();
        }

        private void CreateGravityWellEffect(double x, double y, double strength, Color color)
        {
            Ellipse flash = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(color),
                Opacity = 1
            };
            Canvas.SetLeft(flash, x - 10);
            Canvas.SetTop(flash, y - 10);
            GameCanvas.Children.Add(flash);

            DoubleAnimation flashScaleX = new DoubleAnimation
            {
                From = 1,
                To = 10,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            DoubleAnimation flashScaleY = new DoubleAnimation
            {
                From = 1,
                To = 10,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            DoubleAnimation flashOpacity = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            ScaleTransform flashScale = new ScaleTransform();
            flash.RenderTransform = flashScale;
            flashScale.BeginAnimation(ScaleTransform.ScaleXProperty, flashScaleX);
            flashScale.BeginAnimation(ScaleTransform.ScaleYProperty, flashScaleY);
            flash.BeginAnimation(Ellipse.OpacityProperty, flashOpacity);

            System.Windows.Threading.DispatcherTimer cleanupFlash = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            cleanupFlash.Tick += (s, args) =>
            {
                GameCanvas.Children.Remove(flash);
                cleanupFlash.Stop();
            };
            cleanupFlash.Start();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void GameOver()
        {
            gameTimer.Stop();
            spawnTimer.Stop();
            bonusTimer.Stop();

            Border gameOverPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            StackPanel panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock gameOverText = new TextBlock
            {
                Text = "⚛ ИГРА ОКОНЧЕНА ⚛",
                FontSize = 48,
                Foreground = new SolidColorBrush(Colors.Red),
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            TextBlock scoreText = new TextBlock
            {
                Text = $"Ваш счёт: {score}",
                FontSize = 32,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };

            Button restartBtn = new Button
            {
                Content = "🔄 НОВАЯ ИГРА",
                Width = 200,
                Height = 50,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.Red),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            restartBtn.Click += (s, a) => ResetGame();

            panel.Children.Add(gameOverText);
            panel.Children.Add(scoreText);
            panel.Children.Add(restartBtn);
            gameOverPanel.Child = panel;

            GameCanvas.Children.Add(gameOverPanel);

            Canvas.SetLeft(gameOverPanel, 0);
            Canvas.SetTop(gameOverPanel, 0);
            gameOverPanel.Width = canvasWidth;
            gameOverPanel.Height = canvasHeight;
        }

        private void ResetGame()
        {
            foreach (var p in particles)
            {
                GameCanvas.Children.Remove(p.Visual);
            }
            foreach (var w in gravityWells)
            {
                GameCanvas.Children.Remove(w.Visual);
            }
            foreach (var b in bonuses)
            {
                GameCanvas.Children.Remove(b.Visual);
                if (b.Visual.Tag is TextBlock text)
                {
                    GameCanvas.Children.Remove(text);
                }
            }

            particles.Clear();
            gravityWells.Clear();
            bonuses.Clear();

            score = 0;
            lives = 3;
            combo = 0;
            comboCount = 0;
            activeBonus = BonusType.None;
            activeBonusTimeLeft = 0;
            shieldActive = false;
            scoreMultiplier = 1.0;

            gameTimer.Start();
            UpdateSpawnTimer();
            spawnTimer.Start();
            bonusTimer.Start();

            for (int i = 0; i < 15; i++)
            {
                SpawnRandomParticle();
            }

            var gameOverPanel = GameCanvas.Children.OfType<Border>().FirstOrDefault();
            if (gameOverPanel != null)
            {
                GameCanvas.Children.Remove(gameOverPanel);
            }

            UpdateUI();
            UpdateParticlesCount();
            UpdateBonusDisplay();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;
            if (activeBonus != BonusType.SlowMotion) // Не меняем скорость при активном замедлении
            {
                UpdateSpeedMultiplier();
            }
        }
    }
}