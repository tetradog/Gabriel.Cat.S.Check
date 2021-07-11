using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;



namespace Gabriel.Cat.S.Check
{
    public delegate Task<IEnumerable<IFile>> GetCapitulosDelegate(Uri uriWeb);
    public delegate void LogDelegate(string log);
    public delegate bool CheckFileExistDelegate(string fileName);
    public delegate void TrataFileDelegate(string fileName,params string[] args);
    public delegate string[] ReadFileDelegate(string fileName);
    public class Check
    {
        public const int DEFAULTTIME = 5 * 60 * 1000;
        
        public Check(string fileConfig = "Config.txt", string fileUploaded = "Uploaded.txt")
        {
            FileConfig = fileConfig;
            DicNames = new SortedList<string, string>();
            FileUploaded = fileUploaded;
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
        public string ChannelName { get; set; }
        public string Channel => $"@{ChannelName}";
        public Uri Web { get; set; }
        public string FileUploaded { get; set; }
        public string FileConfig { get; set; }

        public SortedList<string, string> DicNames { get; set; }
        public TelegramBotClient BotClient { get; set; }

        public void Load(string[] args = default)
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
            ChannelName = args[1];
            ApiKey = args[2];
            if (args.Length > CAMPOSOBLIGATORIOS)
                FileUploaded = args[3];
            if (ExistFile(FileConfig))
            {
               DeleteFile(FileConfig);
            }
            AppendFile(FileConfig, args);

            if (ExistFile(FileUploaded))
            {
                foreach (string capitulo in ReadFile(FileUploaded))
                {
                    DicNames.Add(capitulo, capitulo);
                }
            }
            BotClient = new TelegramBotClient(ApiKey);
        }

        public async Task PublicarUnaVez([NotNull] GetCapitulosDelegate method)
        {
            IEnumerable<Link> linkMega;
            foreach (IFile capitulo in await method(Web))
            {
                if (!DicNames.ContainsKey(capitulo.Name))
                {
                    DicNames.Add(capitulo.Name, capitulo.Name);
                    linkMega = capitulo.GetLinks();
                    if (!Equals(linkMega, default) && linkMega.Count() > 0)
                    {
                        await BotClient.SendPhotoAsync(Channel, new Telegram.Bot.Types.InputFiles.InputOnlineFile(capitulo.Picture), $"{capitulo.Name} \n{string.Join('\n', linkMega.Select(l=>$"{l.TextoAntes}{l.Url}{l.TextoDespues}"))}");
                        Log(capitulo.Name);
                        AppendFile(FileUploaded, capitulo.Name);
                    }
                    else DicNames.Remove(capitulo.Name);
                }
            }
        }

        public async Task Publicar([NotNull] GetCapitulosDelegate method, int milisegundosAEsperar = -1, int milisegundosTrasError = -1, string mensajePosPublicacion = "Descanso", Cancelation cancelation = default)
        {
            if (milisegundosAEsperar < 0)
                milisegundosAEsperar = DEFAULTTIME;
            if (milisegundosTrasError < 0)
                milisegundosTrasError = DEFAULTTIME;

            if (Equals(cancelation, default))
                cancelation = new Cancelation();

            do
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

            } while (cancelation.Continue);
        }



    }
    public class Cancelation
    {
        public bool Continue { get; set; } = true;
    }
}
