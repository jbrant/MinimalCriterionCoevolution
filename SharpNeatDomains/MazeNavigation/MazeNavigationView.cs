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

        const PixelFormat ViewportPixelFormat = PixelFormat.Format16bppRgb565;

        public MazeNavigationView()
        {
            InitializeComponent();
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