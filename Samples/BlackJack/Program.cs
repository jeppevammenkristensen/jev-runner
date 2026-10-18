using JRK.JevRunner;

Game game = new Game(JevRunner.Init(Environment.GetEnvironmentVariable("JEV_TOKEN")!));

await game.Play();