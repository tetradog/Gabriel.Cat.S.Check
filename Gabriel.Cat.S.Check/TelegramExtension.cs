using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Gabriel.Cat.S.Extension
{
    public static class TelegramExtension
    {
        public static Uri GetLink(this Chat chat) => new Uri("http://t.me/"+ chat.Username);
        public static Uri GetMessageLink(this Chat chat,int idMessage)=> new Uri(System.IO.Path.Combine(chat.GetLink().AbsoluteUri, idMessage + ""));
        public static async Task<string> GetMessage(this Chat chat,int idMessage)
        {
            string htmlPage;
            Uri urlMessage = chat.GetMessageLink(idMessage);
            string result = default;
            try
            {
                htmlPage = await urlMessage.DownloadString();
                /*
                     strMeta=htmlWeb.text.split("<meta name=\"twitter:description\"")[1];
                     strMeta=strMeta.s;
                     content= strMeta.s;
                 
                 */
                result = htmlPage.Split("<meta name=\"twitter:description\"")[1].Split("content=\"")[1].Split("\"")[0];
            }
            catch { }


            return result;
        }
    }
}
