using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace streaming_images_server
{
    public partial class Form1 : Form
    {
        private byte[] bImagem;
        
        public Form1()
        {
            InitializeComponent();
        }

        /* http://csharpexamples.com/c-resize-bitmap-example/ 
         
            Essa função alem de redimensionar a imagem, remove 
            infomações contidas no arquivo, diminuindo o tamanho do arquivo.
            Porém, caso o arquivo não possua nenhuma infomação adicional, nada deve mudar. 

             */
        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        private void func()
        {
            try
            {
                //WebRequest request = WebRequest.Create("http://192.168.25.76/axis-cgi/jpg/image.cgi");
                //WebRequest request = WebRequest.Create("http://192.168.25.76/axis-cgi/mjpg/video.cgi");

                /*WebResponse response = request.GetResponse();                
                Console.WriteLine("2");
                Stream responseStream = response.GetResponseStream();                
                Console.WriteLine("3");*/

                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(new IPEndPoint(IPAddress.Parse("192.168.25.76"), 80));
                MessageBox.Show("Client is connected.\n");

                /* envia requisição de video para a câmera */
                string GETRequest = "GET /axis-cgi/mjpg/video.cgi HTTP/1.1\r\nHost: 192.168.25.76\r\nConnection: Keep-Alive\r\n\r\n";
                clientSocket.Send(Encoding.ASCII.GetBytes(GETRequest));

                /* buffer cabeçalho */
                byte[] cab_buff = new byte[1];

                /* string do cabeçalho */
                string cab_text = "";

                /* tamanho do conteudo(payload)*/
                int contentLength = 0;

                /* buffer conteudo */
                byte[] cont_buff = new byte[0];

                using (Stream netStream = new NetworkStream(clientSocket, true), bufStream = new BufferedStream(netStream, 100000))
                {
                    int num_images = 0;
                    FileStream fs = new FileStream(@"C:\Users\User\Desktop\images\image_" + num_images + ".jfif", FileMode.Append, FileAccess.Write);
                    while(num_images < 50)
                    {
                            
                        if (bufStream.CanRead)
                        {
                            bool primeiro = true;

                            /* le o cabeçalho do pacote recebido byte por byte e monta a string de cabeçalho */
                            while (true)
                            {
                                                                
                                clientSocket.Receive(cab_buff, 0, 1, SocketFlags.None);
                                cab_text += ASCIIEncoding.ASCII.GetString(cab_buff);

                                /* verifica se o cabeçalho ja contem o tamanho do frame */

                                /* extrai o tamanho do frame e cria um buffer desse tamanho para criar a imagem */

                                /* le até frame, incrementando por fragmento (tamanho do fragmento é de 1460) */
                                
                                /* tratar bytes que sobrarem */

                                /* ler o tamanho do proximo frame */


                                /*                               if (!primeiro && cab_text.Contains("\r\n\r\n"))
                                                               {
                                                                   // header is received, parsing content length
                                                                   // I use regular expressions, but any other method you can think of is ok
                                                                   Regex reg = new Regex("\\\r\nContent-Length: (.*?)\\\r\n");

                                                                   Console.WriteLine(cab_text);
                                                                   Match m = reg.Match(cab_text);

                                                                   if (m.Success)
                                                                   {
                                                                       contentLength = int.Parse(m.Groups[1].ToString());

                                                                       Console.WriteLine("content length " + contentLength);

                                                                       // read the body
                                                                       cont_buff = new byte[contentLength];

                                                                       //bufStream.Read(cont_buff, 0, contentLength);

                                                                       int numBytesToRead = contentLength;
                                                                       int bytesReceived = 0;

                                                                       while (numBytesToRead > 0)
                                                                       {
                                                                           // Read may return anything from 0 to numBytesToRead.
                                                                           int n = bufStream.Read(cont_buff, 0, contentLength);

                                                                           // The end of the file is reached.
                                                                           if (n == 0)
                                                                               break;
                                                                           bytesReceived += n;
                                                                           numBytesToRead -= n;
                                                                       }

                                                                       Console.WriteLine(numBytesToRead);
                                                                       Console.WriteLine(bytesReceived);

                                                                       fs.Write(cont_buff, 0, contentLength);
                                                                       fs.Close();
                                                                       break;

                                                                   }
                                                                   else
                                                                   {
                                                                       Console.WriteLine("content length não encontrado ");
                                                                       cab_text = "";
                                                                       primeiro = true;
                                                                   }                                    


                                                               }
                                                               */
                                // read the body
                                cont_buff = new byte[2048];

                                //bufStream.Read(cont_buff, 0, contentLength);

                                int numBytesToRead = 1460;
                                int bytesReceived = 0;

                                while (numBytesToRead > 0)
                                {
                                    // Read may return anything from 0 to numBytesToRead.
                                    int n = bufStream.Read(cont_buff, 0, 1460);

                                    /* insere 10 quebra de linha para facilitar identificação de blocos de conteudo */
                                    for (int i = 1460; i < 1470; i++)
                                        cont_buff[i] = ASCIIEncoding.ASCII.GetBytes("\n")[0];


                                    // The end of the file is reached.
                                    if (n == 0)
                                        break;

                                    bytesReceived += n;
                                    numBytesToRead -= n;
                                }

                                Console.WriteLine(numBytesToRead);
                                Console.WriteLine(bytesReceived);

                                fs.Write(cont_buff, 0, 2048);
                                //fs.Close();

                            }
                        }                        
                        num_images++;
                    }
                    fs.Close();
                    MessageBox.Show("\nShutting down the connection.");
                    bufStream.Close();
                }                                               
            }
            catch (Exception we)
            {                
                Console.WriteLine(we.Message);
                Console.WriteLine(we.StackTrace);
            }
        }


        private void btnCarregar_Click(object sender, EventArgs e)
        {

            func();
            return;

            OpenFileDialog open = new OpenFileDialog();

            // image filters  
            open.Filter = "Image Files(*.jpg; *.jpeg;)|*.jpg; *.jpeg;";


            if (open.ShowDialog() == DialogResult.OK)
            {
                /* cria bitmap do arquivo selecionado, contudo não é 
                 * obrigatório fazer o redimensionamento.
                 */
                Bitmap bmp = ResizeBitmap(new Bitmap(open.FileName), 500, 500);

                /* variavel que armazenará os bytes da imagem */
                byte[] bmp_in_byte;

                /* https://stackoverflow.com/questions/12645705/c-bitmap-to-byte-array */
                using (var memoryStream = new MemoryStream())
                {
                    /* salva a imagem no stream de memória */
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                    /* transforma o stream em array */
                    bmp_in_byte = memoryStream.ToArray();

                    /* copia imagem para enviar posteriormente */
                    this.bImagem = bmp_in_byte;

                    /* apenas liberando recursos da variavel que não sera mais usada */
                    bmp.Dispose();
                }

                /* escreve os bytes recebidos do stream
                 * apenas para verificar o conteúdo, não é necessário                  
                 */
                SaveFileDialog salvarArquivo = new SaveFileDialog();

                salvarArquivo.FileName = "new_image";
                salvarArquivo.DefaultExt = "jpg";
                if (salvarArquivo.ShowDialog() == DialogResult.OK && salvarArquivo.FileName.Length > 0)
                {
                    File.WriteAllBytes(salvarArquivo.FileName, bmp_in_byte);
                    MessageBox.Show("Arquivo criado!");
                }


                /* Fazendo processo inverso
                 * Agora transformando o array de bytes em um stream, e criando uma imagem a partir
                 * desse stream.
                 */
                Bitmap bmp_2 = new Bitmap(new MemoryStream(bmp_in_byte));

                /* mostra a imagem */
                pictureBox1.Image = bmp_2;
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            /* https://docs.microsoft.com/pt-br/dotnet/api/system.io.bufferedstream?view=netframework-4.8#moniker-applies-to */
            /* SERVER SIDE */

            Socket serverSocket;

            /* bytes para envio */
            byte[] dataToSend = this.bImagem;

            /* IP local(server) e porta */
            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("192.168.25.69"), 1800);

            using (Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listenSocket.Bind(iPEndPoint);
                listenSocket.Listen(1);

                serverSocket = listenSocket.Accept();

                Console.WriteLine("server is connected!");
            }

            /* enviando os bytes */
            try
            {
                Console.WriteLine("Sending data...");
                int bytesSent = serverSocket.Send(dataToSend, 0, dataToSend.Length, SocketFlags.None);
                Console.WriteLine("{0} bytes sent", bytesSent.ToString());

            }
            finally
            {
                serverSocket.Shutdown(SocketShutdown.Both);
                Console.WriteLine("Connection shut down.");
                serverSocket.Close();
            }
        }
    }
}
