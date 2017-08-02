namespace MazeExperimentSupportLib
{
    /// <summary>
    ///     Encapsulates a single measure of cluster diversity.
    /// </summary>
    public struct ClusterDiversityUnit
    {
        /// <summary>
        ///     Cluster statistics unit constructor.
        /// </summary>
        /// <param name="numClusters">The number of clusters need to reduce intracluster variance to the specified threshold.</param>
        /// <param name="silhouetteWidth">The silhouette width of the resultant clustering (indicates "goodness" of clustering).</param>
        /// <param name="populationEntropy">The entropy (diversity) of the population based on cluster assignment proportions.</param>
        public ClusterDiversityUnit(int numClusters, double silhouetteWidth, double populationEntropy)
        {
            NumClusters = numClusters;
            SilhouetteWidth = silhouetteWidth;
            PopulationEntropy = populationEntropy;
        }

        /// <summary>
        ///     The number of clusters need to reduce intracluster variance to the specified threshold.
        /// </summary>
        public int NumClusters { get; set; }

        /// <summary>
        ///     The silhouette width of the resultant clustering (indicates "goodness" of clustering).
        /// </summary>
        public double SilhouetteWidth { get; set; }

        /// <summary>
        ///     The entropy (diversity) of the population based on cluster assignment proportions.
        /// </summary>
        public double PopulationEntropy { get; set; }
    }
}