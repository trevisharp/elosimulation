using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

const int startrating = 1000;
const double abilitymax_avg = 200.0;
const double abilitymax_std = 20.0;
const double ability_avg = 100.0;
const double ability_std = 10.0;
const double inconsistency_avg = 5.0;
const double inconsistency_std = 1.0;
const double evolutionrate_avg = 0.01;
const double evolutionrate_std = 0.01;
const double evolutionvariation_avg = 0.005;
const double evolutionvariation_std = 0.001;
const int playercount = 1000;
const int dailymatches = 100;

(int, int) updateelo(int winrating, int loserating, bool draw = false)
{
    if (draw)
    {
        if (winrating - 50 > loserating)
            return (-1, 1);
        else if (loserating - 50 > winrating)
            return (1, -1);
        else return (0, 0);
    }
    else
    {
        if (winrating - 50 > loserating)
            return (6, -6);
        else if (loserating - 50 > winrating)
            return (10, -10);
        else return (8, -8);
    }
}

Random rand = new Random(DateTime.Now.Millisecond);

double normal()
{
    var u = rand.NextDouble() + double.Epsilon;
    var v = rand.NextDouble() + double.Epsilon;
    return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
}

Player newplayer()
{
    Player player = new Player();
    player.Rating = startrating;
    player.Ability = ability_std * normal() + ability_avg;
    player.Inconsistency = inconsistency_std * normal() + inconsistency_avg;
    player.EvolutionRate = evolutionrate_std * normal() + evolutionrate_avg;
    player.EvolutionVariation = evolutionvariation_std * normal() + evolutionvariation_avg;
    player.MaxAbility = abilitymax_std * normal() + abilitymax_avg;
    return player;
}

double perform(Player player)
{
    return player.Inconsistency * normal() + player.Ability;
}

void match(Player p1, Player p2)
{
    var a1 = perform(p1);
    var a2 = perform(p2);
    if (a1 - inconsistency_avg / 4 > a2)
    {
        var r = updateelo(p1.Rating, p2.Rating);
        p1.Rating += r.Item1;
        p2.Rating += r.Item2;
    }
    else if (a2 - inconsistency_avg / 4 > a1)
    {
        var r = updateelo(p2.Rating, p1.Rating);
        p2.Rating += r.Item1;
        p1.Rating += r.Item2;
    }
    else
    {
        var r = updateelo(p1.Rating, p2.Rating, true);
        p1.Rating += r.Item1;
        p2.Rating += r.Item2;
    }
}

void evolve(Player p)
{
    var evolution = p.EvolutionVariation * normal() + p.EvolutionRate;
    p.Ability += (p.MaxAbility - p.Ability) * evolution;
}

void runday(IEnumerable<Player> players, int dailymatchs)
{
    var count = players.Count();
    while (dailymatchs-- > 0)
    {
        var i = rand.Next(0, count);
        var p1 = players.Skip(i).First();
        var j = i + rand.Next(0, 10) - 5;
        if (i == j)
            j += 2 * rand.Next(0, 2) - 1;
        if (j >= count)
            continue;
        var p2 = players.Skip(j).First();
        match(p1, p2);
        evolve(p1);
        evolve(p2);
    }
}

List<Player> players = new List<Player>();
for (int i = 0; i < playercount; i++)
    players.Add(newplayer());

int[] run()
{
    runday(players, dailymatches);
    int[] acc = new int[40];
    players = players.OrderBy(p => p.Rating).ToList();
    foreach (var player in players)
    {
        if (player.Rating is > 3999 or < 0)
            continue;
        acc[player.Rating / 100] += player.Rating;
    }
    return acc;
}

Application.SetHighDpiMode(HighDpiMode.SystemAware);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
var form = new Form();
form.FormBorderStyle = FormBorderStyle.None;
form.WindowState = FormWindowState.Maximized;
Color bg = Color.FromArgb(10, 10, 12);
PictureBox pb = new PictureBox();
pb.Dock = DockStyle.Fill;
Timer timer = new Timer();
timer.Interval = 100;
Bitmap bmp = null;
Graphics g = null;
form.Controls.Add(pb);
form.KeyPreview = true;

form.KeyDown += (obj, e) =>
{
    if (e.KeyCode == Keys.Escape)
        Application.Exit();
};

form.Load += delegate
{
    bmp = new Bitmap(pb.Width, pb.Height);
    g = Graphics.FromImage(bmp);
    g.Clear(bg);
    pb.Image = bmp;
    timer.Start();
};

int days = 0;
timer.Tick += delegate
{
    g.Clear(bg);

    var data = run();
    days++;
    g.DrawString($"Day: {days}", form.Font, Brushes.White, new PointF(15, pb.Height - 85));

    g.DrawLine(Pens.White, 50, pb.Height - 100, pb.Width - 50, pb.Height - 100);

    for (int i = 0; i < 40; i++)
    {
        var x = 55 + i * (pb.Width - 100) / 39;
        var y = (int)((pb.Height - 200) * data[i] / 50000.0);
        g.FillRectangle(Brushes.Aqua, x, pb.Height - y - 100, (pb.Width - 100) / 39 - 10, y);
    }

    pb.Image = bmp;
};
Application.Run(form);

class Player
{
    public int Rating { get; set; }
    public double Ability { get; set; }
    public double MaxAbility { get; set; }
    public double Inconsistency { get; set; }
    public double EvolutionRate { get; set; }
    public double EvolutionVariation { get; set; }
}