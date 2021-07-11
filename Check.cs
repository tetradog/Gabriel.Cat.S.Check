using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Telegram.Bot;

namespace Gabriel.Cat.S.Check
{
    public interface IFileMega
    {
        string Name { get; }
        Uri Picture { get; }
        string[] GetLinksMega();
    }
    public delegate IEnumerable<IFileMega> GetCapitulosDelegate(); 
    public class Check
    {
        public const int DEFAULTTIMETOCHECK=5 * 60 * 1000;
        public Check(string fileConfig = "Config.txt", string fileUploaded = "Uploaded.txt")
        {
            FileConfig = fileConfig;
            DicNames = new SortedList<string, string>();
            FileUploaded = fileUploaded;
        }
        public string ApiKey { get; set; }
        public string ChannelName { get; set; }
        public string Channel => $"@{ChannelName}";
        public Uri Web { get; set; }
        public string FileUploaded { get; set; }
        public string FileConfig { get; set; }

        public SortedList<string, string> DicNames { get; set; }
        public TelegramBotClient BotClient { get;  set; }

        public void Load(string[] args=default)
        {
            const int CAMPOSOBLIGATORIOS = 3;

            if (Equals(args, default))
                args = new string[0];

            if (System.IO.File.Exists(FileConfig))
            {
                args = System.IO.File.ReadAllLines(FileConfig);
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

            if (System.IO.File.Exists(FileUploaded))
            {
                foreach(string capitulo in System.IO.File.ReadAllLines(FileUploaded))
                {
                    DicNames.Add(capitulo, capitulo);
                }
            }
            BotClient = new TelegramBotClient(ApiKey);
        }

        public void PublicarUnaVez([NotNull]GetCapitulosDelegate method)
        {
            string[] linkMega;
            foreach(IFileMega capitulo in method())
            {
                if (!DicNames.ContainsKey(capitulo.Name))
                {
                    DicNames.Add(capitulo.Name, capitulo.Name);
                    linkMega = capitulo.GetLinksMega();
                    if (!Equals(linkMega, default))
                    {
                        BotClient.SendPhotoAsync(Channel, new Telegram.Bot.Types.InputFiles.InputOnlineFile(capitulo.Picture), $"{capitulo.Name} {string.Join('\n',linkMega)}");
                        Console.WriteLine(capitulo.Name);
                        System.IO.File.AppendAllLines(FileUploaded, new string[] { capitulo.Name });
                    }
                    else DicNames.Remove(capitulo.Name);
                }
            }
        }

        public void Publicar([NotNull]GetCapitulosDelegate method,int milisegundosAEsperar=-1,string mensajePosPublicacion="Descanso",Cancelation cancelation=default )
        {
            if (milisegundosAEsperar < 0)
                milisegundosAEsperar = DEFAULTTIMETOCHECK;

            if (Equals(cancelation, default))
                cancelation = new Cancelation();

            do
            {
                PublicarUnaVez(method);
                Console.WriteLine(mensajePosPublicacion);
                System.Threading.Thread.Sleep(milisegundosAEsperar);
               
            } while (cancelation.Continue);
        }



    }
    public class Cancelation
    {
        public bool Continue { get; set; } = true;
    }
}
