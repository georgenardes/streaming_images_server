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
using System.Threading;
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

        private static void salvaArquivo(byte [] buf, int id, int qtd_bytes)
        {
            Console.WriteLine("salvando arquivo");
            FileStream fs = new FileStream(@"C:\Users\User\Desktop\images\_image" + id + ".jfif", FileMode.Append, FileAccess.Write);
            fs.Write(buf, 0, qtd_bytes);
            fs.Write(ASCIIEncoding.ASCII.GetBytes("\n\n\n\n\n"), 0, 5);
            fs.Close();
        }

        /* verifica se o cabeçalho ja contem o tamanho do frame */

        /* extrai o tamanho do frame e cria um buffer desse tamanho para criar a imagem */

        /* le até frame, incrementando por fragmento (tamanho do fragmento é de 1460) */

        /* tratar bytes que sobrarem */

        /* ler o tamanho do proximo frame */

        private void func()
        {
            try
            {
                Socket client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client_socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.25.76"), 80));
                Console.WriteLine("Client is connected.\n");

                /* envia requisição de video para a câmera */
                string GETRequest = "GET /axis-cgi/mjpg/video.cgi HTTP/1.1\r\nHost: 192.168.25.76\r\nConnection: Keep-Alive\r\n\r\n";
                client_socket.Send(Encoding.ASCII.GetBytes(GETRequest));

                /* buffer cabeçalho */
                byte[] cab_buff = new byte[1];

                /* string do cabeçalho */
                string cab_text = "";

                /* tamanho do conteudo(payload)*/
                int content_length = 0;

                /* buffer conteudo */
                byte[] cont_buff = new byte[0];

                /* quantidade de bytes lidos */
                int qtd_b_lido = 0;

                using (Stream netStream = new NetworkStream(client_socket, true), bufStream = new BufferedStream(netStream, 100000), memoryStream = new MemoryStream())
                {                                               
                    if (bufStream.CanRead)
                    {
                        int num_laco = 0;

                        /* le o cabeçalho do pacote recebido byte por byte e monta a string de cabeçalho */
                        while (true)
                        {
                                                                
                            client_socket.Receive(cab_buff, 0, 1, SocketFlags.None);
                            cab_text += ASCIIEncoding.ASCII.GetString(cab_buff);

                            /* verifica se o cabeçalho ja contem o tamanho do frame */                            
                            /* final de cabeçalho */
                            if (cab_text.Contains("\r\n\r\n"))
                            {
                                /* verifica se o cabeçalho contem o tamanho, se não contem, limpa e continua leitura */
                                if (!cab_text.Contains("Content-Length:"))
                                {
                                    //Console.WriteLine("laco " + num_laco + " -- " + cab_text);
                                    cab_text = "";
                                    continue;
                                } else
                                {
                                    /* extrai o tamanho do frame e cria um buffer desse tamanho para criar a imagem */

                                    //Console.WriteLine("2 " + cab_text);

                                    Regex reg = new Regex("\\\r\nContent-Length: (.*?)\\\r\n");
                                    Match m = reg.Match(cab_text);


                                    if (m.Success)
                                    {
                                        /* tamanho total em bytes da imagem que será recebida */
                                        content_length = int.Parse(m.Groups[1].ToString());
                                        //Console.WriteLine("content length " + content_length);

                                        cont_buff = new byte[content_length];

                                        qtd_b_lido = 0;

                                        while (qtd_b_lido < content_length - 1)
                                        {
                                            if (qtd_b_lido + 1460 < content_length)
                                            {
                                                bufStream.Read(cont_buff, qtd_b_lido, 1460);
                                                qtd_b_lido += 1460;
                                            }
                                            else
                                            {
                                                bufStream.Read(cont_buff, qtd_b_lido, content_length - qtd_b_lido);
                                                qtd_b_lido = content_length;
                                            }
                                        }

                                        memoryStream.Write(cont_buff, 0, cont_buff.Length);
                                        pictureBox1.Image = Image.FromStream(memoryStream);

                                        Console.WriteLine("imagem criada " + num_laco);
                                        Thread.Sleep(300);

                                        memoryStream.Flush();

                                        salvaArquivo(cont_buff, num_laco, cont_buff.Length);

                                        /* teoricamente aqui ja teria lido todo o  fragmento do primeiro cabeçalho*/
                                        /* fazer a leitura do proximo cabeçalho e repetir */
                                        //break;
                                        cab_text = "";
                                        num_laco++;
                                    }
                                    else
                                    {
                                        Console.WriteLine("content length não encontrado ");
                                        cab_text = "";
                                        continue;
                                    }

                                }
                            }
                        }
                    }                        
                                            
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
