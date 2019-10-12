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

            modelBuilder.Entity<RunPhase>(entity =>
            {
                entity.Property(e => e.RunPhaseId).HasColumnName("RunPhaseID");

                entity.Property(e => e.RunPhaseName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });
        }
    }
}
