using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using SharpNeat.Core;
using SharpNeat.Domains.PreyCapture;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    public partial class MazeNavigationView : AbstractDomainView
    {
        /// <summary>
        ///     Event that signals simulation thread to start a simulation.
        /// </summary>
        private readonly AutoResetEvent _simStartEvent = new AutoResetEvent(false);

        /// <summary>
        ///     The agent used by the simulation thread.
        /// </summary>
        private IBlackBox _agent;

        private IGenomeDecoder<NeatGenome, IBlackBox> _genomeDecoder;

        /// <summary>
        ///     Indicates is a simulation is running. Access is thread synchronised using Interlocked.
        /// </summary>
        private int _simRunningFlag;

        /// <summary>
        ///     Thread for running simulation.
        /// </summary>
        private Thread _simThread;

        Image _image;

        private MazeNavigationWorld<ITrialInfo> _world; 

        const PixelFormat ViewportPixelFormat = PixelFormat.Format16bppRgb565;

        private bool _initializing = true;

        public MazeNavigationView(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, MazeNavigationWorld<ITrialInfo> world) 
        {
            InitializeComponent();

            _genomeDecoder = genomeDecoder;
            _world = world;

            // Create a bitmap for the picturebox.
            int width = Width;
            int height = Height;
            _image = new Bitmap(width, height, ViewportPixelFormat);
            pbx.Image = _image;

            // Create background thread for running simulation alongside NEAT algorithm.
            //_simThread = new Thread(new ThreadStart(SimulationThread));
            //_simThread.IsBackground = true;
            //_simThread.Start();
        }

        private void PaintView()
        {
            if (_initializing)
            {
                return;
            }            
        }

        private void pbx_SizeChanged(object sender, System.EventArgs e)
        {
            const float ImageSizeChangeDelta = 100f;

            if (_initializing)
            {
                return;
            }

            // Track viewport area.
            int width = Width;
            int height = Height;

            // If the viewport has grown beyond the size of the image then create a new image. 
            // Note. If the viewport shrinks we just paint on the existing (larger) image, this prevents unnecessary 
            // and expensive construction/destrucion of Image objects.
            if (width > _image.Width || height > _image.Height)
            {   // Reset the image's size. We round up the the nearest __imageSizeChangeDelta. This prevents unnecessary 
                // and expensive construction/destrucion of Image objects as the viewport is resized multiple times.
                int imageWidth = (int)(Math.Ceiling((float)width / ImageSizeChangeDelta) * ImageSizeChangeDelta);
                int imageHeight = (int)(Math.Ceiling((float)height / ImageSizeChangeDelta) * ImageSizeChangeDelta);
                _image = new Bitmap(imageWidth, imageHeight, ViewportPixelFormat);
                pbx.Image = _image;
            }

            // Repaint control.
            if (null != _world)
            {
                PaintView();
            }
        }

        public override void RefreshView(object genome)
        {
            // Zero indicates that the simulation is not currently running.
            if (0 == Interlocked.Exchange(ref _simRunningFlag, 1))
            {
                // We got the lock. Decode the genome and store resuly in an instance field.
                var neatGenome = genome as NeatGenome;
                _agent = _genomeDecoder.Decode(neatGenome);

                // Signal simulation thread to start running a simulation.
                _simStartEvent.Set();
            }
        }
    }
}