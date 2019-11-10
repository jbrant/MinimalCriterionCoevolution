using ExperimentEntities.entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExperimentEntities
{
    public partial class ExperimentDataContext : DbContext
    {
        public ExperimentDataContext()
        {
        }

        public ExperimentDataContext(DbContextOptions<ExperimentDataContext> options)
            : base(options)
        {
        }
        
        public virtual DbSet<ExperimentDictionary> ExperimentDictionary { get; set; }
        public virtual DbSet<ExperimentDictionaryBodyBrain> ExperimentDictionaryBodyBrain { get; set; }
        public virtual DbSet<MccexperimentExtantMazePopulation> MccexperimentExtantMazePopulation { get; set; }
        public virtual DbSet<MccexperimentExtantNavigatorPopulation> MccexperimentExtantNavigatorPopulation { get; set; }
        public virtual DbSet<MccexperimentMazeEvaluationData> MccexperimentMazeEvaluationData { get; set; }
        public virtual DbSet<MccexperimentMazeGenome> MccexperimentMazeGenomes { get; set; }
        public virtual DbSet<MccexperimentMazeResourceUsage> MccexperimentMazeResourceUsage { get; set; }
        public virtual DbSet<MccexperimentMazeTrials> MccexperimentMazeTrials { get; set; }
        public virtual DbSet<MccexperimentNavigatorEvaluationData> MccexperimentNavigatorEvaluationData { get; set; }
        public virtual DbSet<MccexperimentNavigatorGenome> MccexperimentNavigatorGenomes { get; set; }
        public virtual DbSet<MccexperimentNavigatorTrials> MccexperimentNavigatorTrials { get; set; }
        public virtual DbSet<MccfullTrajectory> MccfullTrajectories { get; set; }
        public virtual DbSet<MccmazeNavigatorResult> MccmazeNavigatorResults { get; set; }
        public virtual DbSet<McctrajectoryDiversity> McctrajectoryDiversity { get; set; }
        public virtual DbSet<McsexperimentEvaluationData> McsexperimentEvaluationData { get; set; }
        public virtual DbSet<McsexperimentOrganismStateData> McsexperimentOrganismStateData { get; set; }
        public virtual DbSet<RunPhase> RunPhase { get; set; }
        
        public virtual DbSet<MccexperimentExtantVoxelBodyPopulation> MccexperimentExtantVoxelBodyPopulation { get; set; }
        public virtual DbSet<MccexperimentExtantVoxelBrainPopulation> MccexperimentExtantVoxelBrainPopulation { get; set; }
        public virtual DbSet<MccexperimentVoxelBodyEvaluationData> MccexperimentVoxelBodyEvaluationData { get; set; }
        public virtual DbSet<MccexperimentVoxelBodyGenome> MccexperimentVoxelBodyGenomes { get; set; }
        public virtual DbSet<MccexperimentVoxelBodyResourceUsage> MccexperimentVoxelBodyResourceUsage { get; set; }
        public virtual DbSet<MccexperimentVoxelBodyTrials> MccexperimentVoxelBodyTrials { get; set; }
        public virtual DbSet<MccexperimentVoxelBrainEvaluationData> MccexperimentVoxelBrainEvaluationData { get; set; }
        public virtual DbSet<MccexperimentVoxelBrainGenome> MccexperimentVoxelBrainGenomes { get; set; }
        public virtual DbSet<MccexperimentVoxelBrainTrials> MccexperimentVoxelBrainTrials { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("ExperimentDbConnection"), 
                    options => options.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<MccexperimentExtantMazePopulation>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId });

                entity.ToTable("MCCExperimentExtantMazePopulation");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.SpecieId).HasColumnName("SpecieID");
            });

            modelBuilder.Entity<MccexperimentExtantNavigatorPopulation>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId, e.RunPhaseFk });

                entity.ToTable("MCCExperimentExtantNavigatorPopulation");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.Property(e => e.SpecieId).HasColumnName("SpecieID");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.MccexperimentExtantNavigatorPopulation)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCCExperimentExtantNavigatorPopulation_RunPhase");
            });

            modelBuilder.Entity<MccexperimentMazeEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation });

                entity.ToTable("MCCExperimentMazeEvaluationData");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");
            });

            modelBuilder.Entity<MccexperimentMazeGenome>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.GenomeId });

                entity.ToTable("MCCExperimentMazeGenomes");

                entity.HasIndex(e => new { e.GenomeId, e.ExperimentDictionaryId, e.Run })
                    .HasName("_dta_index_MCCExperimentMazeGenomes_5_488388809__K1_K2_3");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.GenomeXml)
                    .IsRequired()
                    .HasColumnType("xml");
            });
            
            modelBuilder.Entity<MccexperimentMazeResourceUsage>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId });

                entity.ToTable("MCCExperimentMazeResourceUsage");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");
            });
            
            modelBuilder.Entity<MccexperimentMazeTrials>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.MazeGenomeId, e.PairedNavigatorGenomeId });

                entity.ToTable("MCCExperimentMazeTrials");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.MazeGenomeId).HasColumnName("MazeGenomeID");

                entity.Property(e => e.PairedNavigatorGenomeId).HasColumnName("PairedNavigatorGenomeID");
            });

            modelBuilder.Entity<MccexperimentNavigatorEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.RunPhaseFk });

                entity.ToTable("MCCExperimentNavigatorEvaluationData");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.MccexperimentNavigatorEvaluationData)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCCExperimentNavigatorEvaluationData_RunPhase");
            });

            modelBuilder.Entity<MccexperimentNavigatorGenome>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.GenomeId, e.RunPhaseFk });

                entity.ToTable("MCCExperimentNavigatorGenomes");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.Property(e => e.GenomeXml)
                    .IsRequired()
                    .HasColumnType("xml");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.MccexperimentNavigatorGenomes)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCCExperimentNavigatorGenomes_RunPhase");
            });
            
            modelBuilder.Entity<MccexperimentNavigatorTrials>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.NavigatorGenomeId, e.PairedMazeGenomeId });

                entity.ToTable("MCCExperimentNavigatorTrials");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.NavigatorGenomeId).HasColumnName("NavigatorGenomeID");

                entity.Property(e => e.PairedMazeGenomeId).HasColumnName("PairedMazeGenomeID");
            });

            modelBuilder.Entity<MccfullTrajectory>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.Timestep, e.MazeGenomeId, e.NavigatorGenomeId })
                    .HasName("PK_CoevolutionMCSFullTrajectories");

                entity.ToTable("MCCFullTrajectories");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.MazeGenomeId).HasColumnName("MazeGenomeID");

                entity.Property(e => e.NavigatorGenomeId).HasColumnName("NavigatorGenomeID");

                entity.Property(e => e.Xposition)
                    .HasColumnName("XPosition")
                    .HasColumnType("numeric(10, 6)");

                entity.Property(e => e.Yposition)
                    .HasColumnName("YPosition")
                    .HasColumnType("numeric(10, 6)");
            });

            modelBuilder.Entity<MccmazeNavigatorResult>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.MazeGenomeId, e.NavigatorGenomeId, e.RunPhaseFk })
                    .HasName("PK_CoevolutionMCSMazeNavigatorResults");

                entity.ToTable("MCCMazeNavigatorResults");

                entity.HasIndex(e => new { e.Generation, e.NavigatorGenomeId, e.NumTimesteps, e.RunPhaseFk, e.MazeGenomeId, e.ExperimentDictionaryId, e.Run, e.IsMazeSolved })
                    .HasName("_dta_index_MCCMazeNavigatorResults_5_406292507__K8_K4_K1_K2_K6_3_5_7");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.MazeGenomeId).HasColumnName("MazeGenomeID");

                entity.Property(e => e.NavigatorGenomeId).HasColumnName("NavigatorGenomeID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.MccmazeNavigatorResults)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CoevolutionMCSMazeNavigatorResults_RunPhase");
            });

            modelBuilder.Entity<McctrajectoryDiversity>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.MazeGenomeId, e.NavigatorGenomeId })
                    .HasName("PK_CoevolutionMCSTrajectoryDiversity");

                entity.ToTable("MCCTrajectoryDiversity");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.MazeGenomeId).HasColumnName("MazeGenomeID");

                entity.Property(e => e.NavigatorGenomeId).HasColumnName("NavigatorGenomeID");
            });

            modelBuilder.Entity<McsexperimentEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.RunPhaseFk, e.TotalEvaluations })
                    .HasName("PK_MCSExperimentEvaluationData_1");

                entity.ToTable("MCSExperimentEvaluationData");

                entity.HasIndex(e => e.ClosestGenomeTotalGeneCount)
                    .HasName("_dta_index_MCSExperimentEvaluationData_5_631673298__K14");

                entity.HasIndex(e => e.ExperimentDictionaryId)
                    .HasName("_dta_index_MCSExperimentEvaluationData_5_631673298__K1");

                entity.HasIndex(e => new { e.ExperimentDictionaryId, e.ViableOffspringCount })
                    .HasName("_dta_index_MCSExperimentEvaluationData_19");

                entity.HasIndex(e => new { e.ClosestGenomeDistanceToTarget, e.ExperimentDictionaryId, e.Generation })
                    .HasName("_dta_index_MCSExperimentEvaluationData_5_631673298__K1_K3_15");

                entity.HasIndex(e => new { e.OffspringCount, e.ClosestGenomeDistanceToTarget, e.ExperimentDictionaryId, e.Generation })
                    .HasName("_dta_index_MCSExperimentEvaluationData_5_631673298__K1_K3_6_15");

                entity.HasIndex(e => new { e.MaxComplexity, e.MeanComplexity, e.TotalEvaluations, e.ClosestGenomeDistanceToTarget, e.ExperimentDictionaryId, e.Run })
                    .HasName("_dta_index_MCSExperimentEvaluationData_5_631673298__K1_K2_7_8_9_15");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.Property(e => e.ClosestGenomeId).HasColumnName("ClosestGenomeID");

                entity.Property(e => e.ClosestGenomeXml).HasColumnType("xml");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.McsexperimentEvaluationData)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCSExperimentEvaluationData_RunPhase");
            });

            modelBuilder.Entity<McsexperimentOrganismStateData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.Evaluation })
                    .HasName("PK_MCSExperimentOrganismStateData_1");

                entity.ToTable("MCSExperimentOrganismStateData");

                entity.HasIndex(e => e.IsViable)
                    .HasName("_dta_index_MCSExperimentOrganismStateData_5_955150448__K7");

                entity.HasIndex(e => new { e.DistanceToTarget, e.ExperimentDictionaryId, e.Generation, e.Run })
                    .HasName("_dta_index_MCSExperimentOrganismStateData_5_955150448__K1_K3_K2_9");

                entity.HasIndex(e => new { e.IsViable, e.ExperimentDictionaryId, e.RunPhaseFk, e.Run, e.Generation })
                    .HasName("_dta_index_MCSExperimentOrganismStateData_5_955150448__K1_K6_K2_K3_7");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.AgentXlocation).HasColumnName("AgentXLocation");

                entity.Property(e => e.AgentYlocation).HasColumnName("AgentYLocation");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.McsexperimentOrganismStateData)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCSExperimentOrganismStateData_RunPhase");
            });

            modelBuilder.Entity<NoveltyExperimentEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation });

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.ChampGenomeId).HasColumnName("ChampGenomeID");
            });

            modelBuilder.Entity<NoveltyExperimentOrganismStateData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.Evaluation });

                entity.HasIndex(e => e.StopConditionSatisfied)
                    .HasName("_dta_index_NoveltyExperimentOrganismStateDa_5_2130106629__K5");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.AgentXlocation).HasColumnName("AgentXLocation");

                entity.Property(e => e.AgentYlocation).HasColumnName("AgentYLocation");
            });
            
            modelBuilder.Entity<ExperimentDictionaryBodyBrain>(entity =>
            {
                entity.HasKey(e => e.ExperimentDictionaryId);

                entity.HasIndex(e => e.ExperimentName)
                    .HasName("UIX_ExperimentDictionaryBodyBrain")
                    .IsUnique();

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.ActivationScheme)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ExperimentName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.InitializationBehaviorCharacterization)
                    .HasColumnName("Initialization_BehaviorCharacterization")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.InitializationComplexityRegulationStrategy)
                    .IsRequired()
                    .HasColumnName("Initialization_ComplexityRegulationStrategy")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.InitializationComplexityThreshold).HasColumnName("Initialization_ComplexityThreshold");

                entity.Property(e => e.InitializationGenomeConfigAddConnnectionProbability).HasColumnName("Initialization_GenomeConfig_AddConnnectionProbability");

                entity.Property(e => e.InitializationGenomeConfigAddNodeProbability).HasColumnName("Initialization_GenomeConfig_AddNodeProbability");

                entity.Property(e => e.InitializationGenomeConfigConnectionWeightRange).HasColumnName("Initialization_GenomeConfig_ConnectionWeightRange");

                entity.Property(e => e.InitializationGenomeConfigDeleteConnectionProbability).HasColumnName("Initialization_GenomeConfig_DeleteConnectionProbability");

                entity.Property(e => e.InitializationGenomeConfigInitialConnectionProportion).HasColumnName("Initialization_GenomeConfig_InitialConnectionProportion");

                entity.Property(e => e.InitializationGenomeConfigWeightMutationProbability).HasColumnName("Initialization_GenomeConfig_WeightMutationProbability");

                entity.Property(e => e.InitializationInterspeciesMatingProbability).HasColumnName("Initialization_InterspeciesMatingProbability");

                entity.Property(e => e.InitializationNearestNeighbors).HasColumnName("Initialization_NearestNeighbors");

                entity.Property(e => e.InitializationNoveltyConfigArchiveAdditionThreshold).HasColumnName("Initialization_NoveltyConfig_ArchiveAdditionThreshold");

                entity.Property(e => e.InitializationNoveltyConfigArchiveThresholdDecreaseMultiplier).HasColumnName("Initialization_NoveltyConfig_ArchiveThresholdDecreaseMultiplier");

                entity.Property(e => e.InitializationNoveltyConfigArchiveThresholdIncreaseMultiplier).HasColumnName("Initialization_NoveltyConfig_ArchiveThresholdIncreaseMultiplier");

                entity.Property(e => e.InitializationNoveltyConfigMaxGenerationalArchiveAddition).HasColumnName("Initialization_NoveltyConfig_MaxGenerationalArchiveAddition");

                entity.Property(e => e.InitializationNoveltyConfigMaxGenerationsWithoutArchiveAddition).HasColumnName("Initialization_NoveltyConfig_MaxGenerationsWithoutArchiveAddition");

                entity.Property(e => e.InitializationOffspringAsexualProbability).HasColumnName("Initialization_OffspringAsexualProbability");

                entity.Property(e => e.InitializationOffspringBatchSize).HasColumnName("Initialization_OffspringBatchSize");

                entity.Property(e => e.InitializationOffspringSexualProbability).HasColumnName("Initialization_OffspringSexualProbability");

                entity.Property(e => e.InitializationPopulationEvaluationFrequency).HasColumnName("Initialization_PopulationEvaluationFrequency");

                entity.Property(e => e.InitializationPopulationSize).HasColumnName("Initialization_PopulationSize");

                entity.Property(e => e.InitializationSearchAlgorithm)
                    .IsRequired()
                    .HasColumnName("Initialization_SearchAlgorithm")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.InitializationSelectionAlgorithm)
                    .IsRequired()
                    .HasColumnName("Initialization_SelectionAlgorithm")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.InitializationSelectionProportion).HasColumnName("Initialization_SelectionProportion");

                entity.Property(e => e.InitializationSpecieCount).HasColumnName("Initialization_SpecieCount");

                entity.Property(e => e.PrimaryBehaviorCharacterization)
                    .IsRequired()
                    .HasColumnName("Primary_BehaviorCharacterization")
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.PrimaryGenomeConfigAddConnnectionProbability).HasColumnName("Primary_GenomeConfig_AddConnnectionProbability");

                entity.Property(e => e.PrimaryGenomeConfigAddNodeProbability).HasColumnName("Primary_GenomeConfig_AddNodeProbability");

                entity.Property(e => e.PrimaryGenomeConfigConnectionWeightRange).HasColumnName("Primary_GenomeConfig_ConnectionWeightRange");

                entity.Property(e => e.PrimaryGenomeConfigDecreaseResolutionProbability).HasColumnName("Primary_GenomeConfig_DecreaseResolutionProbability");

                entity.Property(e => e.PrimaryGenomeConfigDeleteConnectionProbability).HasColumnName("Primary_GenomeConfig_DeleteConnectionProbability");

                entity.Property(e => e.PrimaryGenomeConfigIncreaseResolutionProbability).HasColumnName("Primary_GenomeConfig_IncreaseResolutionProbability");

                entity.Property(e => e.PrimaryGenomeConfigInitialConnectionProportion).HasColumnName("Primary_GenomeConfig_InitialConnectionProportion");

                entity.Property(e => e.PrimaryGenomeConfigWeightMutationProbability).HasColumnName("Primary_GenomeConfig_WeightMutationProbability");

                entity.Property(e => e.VoxelyzeConfigActuationsPerSecond).HasColumnName("VoxelyzeConfig_ActuationsPerSecond");

                entity.Property(e => e.VoxelyzeConfigBrainNetworkConnections).HasColumnName("VoxelyzeConfig_BrainNetworkConnections");

                entity.Property(e => e.VoxelyzeConfigFloorSlope).HasColumnName("VoxelyzeConfig_FloorSlope");

                entity.Property(e => e.VoxelyzeConfigInitialXdimension).HasColumnName("VoxelyzeConfig_InitialXDimension");

                entity.Property(e => e.VoxelyzeConfigInitialYdimension).HasColumnName("VoxelyzeConfig_InitialYDimension");

                entity.Property(e => e.VoxelyzeConfigInitialZdimension).HasColumnName("VoxelyzeConfig_InitialZDimension");

                entity.Property(e => e.VoxelyzeConfigInitializationSeconds).HasColumnName("VoxelyzeConfig_InitializationSeconds");

                entity.Property(e => e.VoxelyzeConfigMinPercentActiveMaterial).HasColumnName("VoxelyzeConfig_MinPercentActiveMaterial");

                entity.Property(e => e.VoxelyzeConfigMinPercentMaterial).HasColumnName("VoxelyzeConfig_MinPercentMaterial");

                entity.Property(e => e.VoxelyzeConfigSimulatedSeconds).HasColumnName("VoxelyzeConfig_SimulatedSeconds");
            });

            modelBuilder.Entity<RunPhase>(entity =>
            {
                entity.Property(e => e.RunPhaseId).HasColumnName("RunPhaseID");

                entity.Property(e => e.RunPhaseName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });
            
            modelBuilder.Entity<MccexperimentExtantVoxelBodyPopulation>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId });

                entity.ToTable("MCCExperimentExtantVoxelBodyPopulation");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");
            });

            modelBuilder.Entity<MccexperimentExtantVoxelBrainPopulation>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId, e.RunPhaseFk });

                entity.ToTable("MCCExperimentExtantVoxelBrainPopulation");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");
            });

            modelBuilder.Entity<MccexperimentVoxelBodyEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation });

                entity.ToTable("MCCExperimentVoxelBodyEvaluationData");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");
            });

            modelBuilder.Entity<MccexperimentVoxelBodyGenome>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.GenomeId });

                entity.ToTable("MCCExperimentVoxelBodyGenomes");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.GenomeXml)
                    .IsRequired()
                    .HasColumnType("xml");
            });

            modelBuilder.Entity<MccexperimentVoxelBodyResourceUsage>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.GenomeId });

                entity.ToTable("MCCExperimentVoxelBodyResourceUsage");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");
            });

            modelBuilder.Entity<MccexperimentVoxelBodyTrials>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.BodyGenomeId, e.PairedBrainGenomeId });

                entity.ToTable("MCCExperimentVoxelBodyTrials");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.BodyGenomeId).HasColumnName("BodyGenomeID");

                entity.Property(e => e.PairedBrainGenomeId).HasColumnName("PairedBrainGenomeID");
            });

            modelBuilder.Entity<MccexperimentVoxelBrainEvaluationData>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation });

                entity.ToTable("MCCExperimentVoxelBrainEvaluationData");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");
            });

            modelBuilder.Entity<MccexperimentVoxelBrainGenome>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.GenomeId, e.RunPhaseFk });

                entity.ToTable("MCCExperimentVoxelBrainGenomes");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.GenomeId).HasColumnName("GenomeID");

                entity.Property(e => e.RunPhaseFk).HasColumnName("RunPhase_FK");

                entity.Property(e => e.GenomeXml)
                    .IsRequired()
                    .HasColumnType("xml");
                
                entity.HasOne(d => d.RunPhaseFkNavigation)
                    .WithMany(p => p.MccexperimentVoxelBrainGenomes)
                    .HasForeignKey(d => d.RunPhaseFk)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MCCExperimentVoxelBrainGenomes_RunPhase");
            });

            modelBuilder.Entity<MccexperimentVoxelBrainTrials>(entity =>
            {
                entity.HasKey(e => new { e.ExperimentDictionaryId, e.Run, e.Generation, e.BrainGenomeId, e.PairedBodyGenomeId });

                entity.ToTable("MCCExperimentVoxelBrainTrials");

                entity.Property(e => e.ExperimentDictionaryId).HasColumnName("ExperimentDictionaryID");

                entity.Property(e => e.BrainGenomeId).HasColumnName("BrainGenomeID");

                entity.Property(e => e.PairedBodyGenomeId).HasColumnName("PairedBodyGenomeID");
            });
        }
    }
}
