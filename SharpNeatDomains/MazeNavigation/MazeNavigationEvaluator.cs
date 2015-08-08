using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Core;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.MazeNavigation
{
    class MazeNavigationEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        #region Class Variables

        // Evaluator state
        private ulong _evaluationCount;
        
        #endregion

        public ulong EvaluationCount
        {
            get { return _evaluationCount; }
        }

        public bool StopConditionSatisfied
        {
            get { throw new NotImplementedException(); }
        }

        public FitnessInfo Evaluate(IBlackBox phenome)
        {

            // Increment eval count
            _evaluationCount++;

            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
