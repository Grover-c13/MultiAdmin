using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MultiAdmin.ConsoleTools;

namespace MultiAdmin.ServerIO
{
	public class OutputHandler
	{
		public static readonly Regex SmodRegex =
			new Regex(@"\[(DEBUG|INFO|WARN|ERROR)\] (\[.*?\]) (.*)", RegexOptions.Compiled | RegexOptions.Singleline);

		private readonly Server server;

		public OutputHandler(Server server)
		{
			this.server = server;
		}

		public void HandleMessage(object source, string message)
		{
			if (message == null)
				return;

			ColoredMessage coloredMessage = new ColoredMessage(message, ConsoleColor.White);

			if (coloredMessage.text.Length > 0)
			{
				if (byte.TryParse(coloredMessage.text[0].ToString(), NumberStyles.HexNumber,
					NumberFormatInfo.CurrentInfo, out byte consoleColor))
				{
					coloredMessage.textColor = (ConsoleColor)consoleColor;
					coloredMessage.text = coloredMessage.text.Substring(1);
				}

				// Smod2 loggers pretty printing
				Match match = SmodRegex.Match(coloredMessage.text);
				if (match.Success)
				{
					if (match.Groups.Count >= 3)
					{
						ConsoleColor levelColor = ConsoleColor.Green;
						ConsoleColor tagColor = ConsoleColor.Yellow;
						ConsoleColor msgColor = coloredMessage.textColor ?? ConsoleColor.White;

						switch (match.Groups[1].Value.Trim())
						{
							case "DEBUG":
								levelColor = ConsoleColor.DarkGray;
								break;

							case "INFO":
								levelColor = ConsoleColor.Green;
								break;

							case "WARN":
								levelColor = ConsoleColor.DarkYellow;
								break;

							case "ERROR":
								levelColor = ConsoleColor.Red;
								break;
						}

						server.Write(
							new[]
							{
								new ColoredMessage($"[{match.Groups[1].Value}] ", levelColor),
								new ColoredMessage($"{match.Groups[2].Value} ", tagColor),
								new ColoredMessage(match.Groups[3].Value, msgColor)
							}, msgColor);

						// P.S. the format is [Info] [courtney.exampleplugin] Something interesting happened
						// That was just an example

						// This return should be here
						return;
					}
				}

				switch (coloredMessage.text.Trim('.', ' ', '\t'))
				{
					case "The round is about to restart! Please wait":
						server.ForEachHandler<IEventRoundEnd>(roundEnd => roundEnd.OnRoundEnd());
						break;

					case "Waiting for players":
						server.IsLoading = false;

						server.ForEachHandler<IEventWaitingForPlayers>(waitingForPlayers =>
							waitingForPlayers.OnWaitingForPlayers());
						break;

					case "New round has been started":
						server.ForEachHandler<IEventRoundStart>(roundStart => roundStart.OnRoundStart());
						break;

					case "Level loaded. Creating match":
						server.ForEachHandler<IEventServerStart>(serverStart => serverStart.OnServerStart());
						break;

					case "Server full":
						server.ForEachHandler<IEventServerFull>(serverFull => serverFull.OnServerFull());
						break;
				}
			}

			server.Write(coloredMessage);
		}
	}
}
