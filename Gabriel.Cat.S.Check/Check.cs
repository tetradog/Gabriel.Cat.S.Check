using Gabriel.Cat.S.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Gabriel.Cat.S.Check
{
    public delegate Task<IEnumerable<IFile>> GetCapitulosDelegate(Uri uriWeb);
    public delegate void LogDelegate(string log);
    public delegate bool CheckFileExistDelegate(string fileName);
    public delegate void TrataFileDelegate(string fileName, params string[] args);
    public delegate string[] ReadFileDelegate(string fileName);
    public class Check
    {
        public const int DEFAULTTIME = 5 * 60 * 1000;
        public const string CHANNELLOG = "@CheckingLog";
        public Check(string fileConfig = "Config.txt")
        {
            FileConfig = fileConfig;
            DicNames = new SortedList<string, int>();
            Log = (s) => Console.WriteLine(s);
            ExistFile = (f) => System.IO.File.Exists(f);
            DeleteFile = (f, args) => System.IO.File.Delete(f);
            AppendFile = (f, args) => System.IO.File.AppendAllLines(f, args);
            ReadFile = (f) => System.IO.File.ReadAllLines(f);
        }
        public LogDelegate Log { get; set; }
        public ReadFileDelegate ReadFile { get; set; }
        public CheckFileExistDelegate ExistFile { get; set; }
        public TrataFileDelegate DeleteFile { get; set; }
        public TrataFileDelegate AppendFile { get; set; }
        public string ApiKey { get; set; }
        public string Channel { get; set; }
        public Uri Web { get; set; }
        public string FileConfig { get; set; }
        public int LogId { get; set; }
        public int TotalLoop { get; set; }

        public SortedList<string, int> DicNames { get; set; }
        public TelegramBotClient BotClient { get; set; }

        public async Task Load(string[] args = default)
        {
            const int CAMPOSOBLIGATORIOS = 3;

            if (Equals(args, default))
                args = new string[0];

            if (ExistFile(FileConfig))
            {
                args = ReadFile(FileConfig);
                if (args.Length < CAMPOSOBLIGATORIOS)
                    throw new Exception("el archivo no contiene todos los elementos, Web,Canal,ApiKeyBot,FileUploaded*");

            }
            else
            {

                if (args.Length < CAMPOSOBLIGATORIOS)
                {
                    if (args.Length > 0)
                    {
                        throw new Exception("se tienen que pasar todos los elementos: Web,Canal,ApiKeyBot,FileUploaded*");
                    }
                    else
                    {
                        throw new Exception("No se ha podido inicializar!!");
                    }
                }
            }

            Web = new Uri(args[0]);
            Channel = args[1];
            if (!Channel.StartsWith('@'))
            {
                Channel = "@" + Channel;
                args[1] = Channel;
            }
            ApiKey = args[2];
            BotClient = new TelegramBotClient(ApiKey);
            if (args.Length > 3)
                TotalLoop = int.Parse(args[3]);
            else
            {
                TotalLoop =-1;
                args = args.Append(TotalLoop + "").ToArray();
            }
            if (args.Length > 4)
                LogId = int.Parse(args[4]);
            else
            {
                LogId = (await BotClient.SendTextMessageAsync(CHANNELLOG, $"Init {Channel}")).MessageId;
                args = args.Append(LogId + "").ToArray();
            }

            if (ExistFile(FileConfig))
            {
                DeleteFile(FileConfig);
            }
            AppendFile(FileConfig, args);

        }

        public async Task PublicarUnaVez([NotNull] GetCapitulosDelegate method)
        {
            string nombreLimpio;
            IEnumerable<Link> linkMega;
            if (DicNames.Count == 0)
            {
                await CargarCapitulosLog();
            }
            foreach (IFile capitulo in await method(Web))
            {
                nombreLimpio = capitulo.Name.Replace("!", "").Trim(' ');
                if (!DicNames.ContainsKey(nombreLimpio))
                {
                    DicNames.Add(nombreLimpio, -1);
                    linkMega = capitulo.GetLinks();
                    if (!Equals(linkMega, default) && linkMega.Count() > 0)
                    {
                        DicNames[capitulo.Name] = (await BotClient.SendPhotoAsync(Channel, new Telegram.Bot.Types.InputFiles.InputOnlineFile(capitulo.Picture), $"{capitulo.Name} \n{string.Join('\n', linkMega.Select(l => $"{l.TextoAntes}{l.Url}{l.TextoDespues}"))}")).MessageId;
                        Log(capitulo.Name);
                        await UpdateLog();
                    }
                    else DicNames.Remove(nombreLimpio);
                }
            }
        }

        private async Task CargarCapitulosLog()
        {
            string[] posts;
            int postId;
            int indexAnd;
            string nombreCapitulo;
            Chat chatLog = await BotClient.GetChatAsync(CHANNELLOG);
            Chat chatChannel = await BotClient.GetChatAsync(Channel);
            string message = await chatLog.GetMessage(LogId);
            if (!string.IsNullOrEmpty(message) && message.Contains("\n") && char.IsDigit(message[0]))
            {
                posts = message.Split('\n');
                for (int i = 0; i < posts.Length; i++)
                {
                    if (int.TryParse(posts[i], out postId))
                    {
                        message = await chatChannel.GetMessage(postId);
                        if (!message.Contains(Channel))
                        {
                            nombreCapitulo = message.Split("\n")[0];
                            do
                            {
                                indexAnd = nombreCapitulo.IndexOf('&');
                                if (indexAnd >= 0)
                                    nombreCapitulo = nombreCapitulo.Remove(indexAnd, nombreCapitulo.IndexOf(';', indexAnd) + 1 - indexAnd);
                            } while (indexAnd >= 0);
                            nombreCapitulo = nombreCapitulo.Trim(' ');
                            if (!DicNames.ContainsKey(nombreCapitulo))
                                DicNames.Add(nombreCapitulo, postId);
                            else
                            {
                                Log($"Duplicado-{nombreCapitulo}");
                                try
                                {
                                    await BotClient.DeleteMessageAsync(Channel, postId);
                                }
                                catch { }

                            }
                        }
                    }
                }

                await UpdateLog();
            }

        }

        private async Task UpdateLog()
        {
            StringBuilder str = new StringBuilder();
            foreach (var item in DicNames)
                str.AppendLine(item.Value + "");
            try { await BotClient.EditMessageTextAsync(CHANNELLOG, LogId, str.ToString()); } catch { }
        }

        public async Task Publicar([NotNull] GetCapitulosDelegate method, int milisegundosAEsperar = -1, int milisegundosTrasError = -1, string mensajePosPublicacion = "Descanso", Cancelation cancelation = default)
        {
            if (milisegundosAEsperar < 0)
                milisegundosAEsperar = DEFAULTTIME;
            if (milisegundosTrasError < 0)
                milisegundosTrasError = DEFAULTTIME;

            if (Equals(cancelation, default))
                cancelation = new Cancelation();

            for(int i=0;i!=TotalLoop &&cancelation.Continue;i++)
            {
                try
                {
                    await PublicarUnaVez(method);
                    Log(mensajePosPublicacion);
                    await Task.Delay(milisegundosAEsperar);
                }
                catch (Exception ex)
                {
                    Log($"Esperando si se resuelve {ex.Message}");
                    await Task.Delay(milisegundosTrasError);
                }

            } 
        }



    }
    public class Cancelation
    {
        public bool Continue { get; set; } = true;
    }
}
