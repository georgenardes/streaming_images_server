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


        private void btnCarregar_Click(object sender, EventArgs e)
        {
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
