using System.Net;
using System.Net.Sockets;

using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.SCTP;
using Sigtran.NET.Layers.TCAP;
using Sigtran.NET.Core.Interfaces;
using Sigtran.NET.Core.Utilities;

Run("SIGTRAN trace formatter emits summaries and hex dumps", SigtranTraceFormatterEmitsSummariesAndHexDumps);
Run("SIGTRAN conformance registry stores vectors deterministically", SigtranConformanceRegistryStoresVectorsDeterministically);
Run("SIGTRAN built-in vectors include M3UA and MAP payloads", SigtranBuiltInVectorsIncludeM3uaAndMapPayloads);
Run("SIGTRAN simulator script emits trace summaries", SigtranSimulatorScriptEmitsTraceSummaries);
Run("MAP SMS simulator flow builds TCAP backed script", MapSmsSimulatorFlowBuildsTcapBackedScript);
Run("SIGTRAN local TCP sample describes M3UA transport", SigtranLocalTcpSampleDescribesM3uaTransport);
Run("SIGTRAN sample catalog exposes supported scenarios", SigtranSampleCatalogExposesSupportedScenarios);
Run("SIGTRAN CI verification profile exposes official commands", SigtranCiVerificationProfileExposesOfficialCommands);
Run("SIGTRAN interoperability readiness reports foundation status", SigtranInteroperabilityReadinessReportsFoundationStatus);
Run("SIGTRAN interoperability tooling status summarizes completed tooling", SigtranInteroperabilityToolingStatusSummarizesCompletedTooling);
Run("SIGTRAN commercial readiness reports release gates", SigtranCommercialReadinessReportsReleaseGates);
Run("SIGTRAN native SCTP support matrix reports verification status", SigtranNativeSctpSupportMatrixReportsVerificationStatus);
Run("SIGTRAN interop evidence registry tracks lab results", SigtranInteropEvidenceRegistryTracksLabResults);
Run("SIGTRAN release candidate manifest reports promotion gates", SigtranReleaseCandidateManifestReportsPromotionGates);
Run("SIGTRAN package governance reports commercial requirements", SigtranPackageGovernanceReportsCommercialRequirements);
Run("SIGTRAN security policy reports response targets", SigtranSecurityPolicyReportsResponseTargets);
Run("SIGTRAN compatibility policy reports SemVer rules", SigtranCompatibilityPolicyReportsSemVerRules);
Run("SIGTRAN observability profile exposes commercial signals", SigtranObservabilityProfileExposesCommercialSignals);
Run("SIGTRAN deployment profiles expose commercial and development gates", SigtranDeploymentProfilesExposeCommercialAndDevelopmentGates);
Run("SIGTRAN commercialization status summarizes foundation", SigtranCommercializationStatusSummarizesCommercializationFoundation);
Run("SIGTRAN interoperability lab scenario catalog exposes required scenarios", SigtranInteropLabScenarioCatalogExposesRequiredScenarios);
Run("SIGTRAN interoperability lab artifact manifest validates required files", SigtranInteropLabArtifactManifestValidatesRequiredFiles);
Run("SIGTRAN interoperability lab run report identifies passing evidence", SigtranInteropLabRunReportIdentifiesPassingEvidence);
Run("SIGTRAN external peer profile exposes M3UA ASP-to-SG template", SigtranExternalPeerInteropProfileExposesM3uaAspToSgTemplate);
Run("SIGTRAN external peer profile marks maintained commercial candidates", SigtranExternalPeerProfileMarksMaintainedCommercialCandidates);
Run("SIGTRAN maintained peer selection policy requires package-neutral evidence criteria", SigtranMaintainedPeerSelectionPolicyRequiresPackageNeutralEvidenceCriteria);
Run("SIGTRAN maintained peer lab binding catalog exposes package-neutral defaults", SigtranMaintainedPeerLabBindingCatalogExposesPackageNeutralDefaults);
Run("SIGTRAN maintained peer lab prerequisites report host readiness", SigtranMaintainedPeerLabPrerequisitesReportHostReadiness);
Run("SIGTRAN maintained peer lab configuration validates environment contracts", SigtranMaintainedPeerLabConfigurationValidatesEnvironmentContracts);
Run("SIGTRAN maintained peer lab artifact plan covers retained evidence paths", SigtranMaintainedPeerLabArtifactPlanCoversRetainedEvidencePaths);
Run("SIGTRAN maintained peer lab command plan covers execution steps", SigtranMaintainedPeerLabCommandPlanCoversExecutionSteps);
Run("SIGTRAN maintained peer lab traffic vectors expose comparable sequence", SigtranMaintainedPeerLabTrafficVectorsExposeComparableSequence);
Run("SIGTRAN maintained peer lab evidence gate requires complete retained artifacts", SigtranMaintainedPeerLabEvidenceGateRequiresCompleteRetainedArtifacts);
Run("SIGTRAN maintained peer lab CI profile is manual and self-hosted", SigtranMaintainedPeerLabCiProfileIsManualAndSelfHosted);
Run("SIGTRAN maintained peer lab status separates foundation from evidence", SigtranMaintainedPeerLabStatusSeparatesFoundationFromEvidence);
Run("SIGTRAN maintained peer lab run manifest aggregates executable contracts", SigtranMaintainedPeerLabRunManifestAggregatesExecutableContracts);
Run("SIGTRAN trace comparison reports ordered mismatches", SigtranTraceComparisonReportsOrderedMismatches);
Run("SIGTRAN interoperability evidence promotion requires passing lab run", SigtranInteropEvidencePromotionRequiresPassingLabRun);
Run("SIGTRAN interoperability lab CI profile is opt-in", SigtranInteropLabCiProfileIsOptIn);
Run("SIGTRAN interoperability lab readiness reports foundation and evidence gates", SigtranInteropLabReadinessReportsFoundationAndEvidenceGates);
Run("SIGTRAN commercial readiness uses interoperability lab production gate", SigtranCommercialReadinessUsesInteropLabProductionGate);
Run("SIGTRAN interoperability lab status summarizes foundation", SigtranInteropLabStatusSummarizesInteropLabFoundation);
Run("SIGTRAN release automation plan exposes deterministic release steps", SigtranReleaseAutomationPlanExposesDeterministicReleaseSteps);
Run("SIGTRAN release artifact manifest tracks package artifacts and digests", SigtranReleaseArtifactManifestTracksPackageArtifactsAndDigests);
Run("SIGTRAN SBOM plan marks commercial release requirement", SigtranSbomPlanMarksCommercialReleaseRequirement);
Run("SIGTRAN package signing plan marks commercial release requirement", SigtranPackageSigningPlanMarksCommercialReleaseRequirement);
Run("SIGTRAN release provenance records source commit and artifact manifest", SigtranReleaseProvenanceRecordsSourceCommitAndArtifactManifest);
Run("SIGTRAN release notes require SemVer and change entries", SigtranReleaseNotesRequireSemVerAndChangeEntries);
Run("SIGTRAN publish channels separate prerelease and stable rules", SigtranPublishChannelsSeparatePrereleaseAndStableRules);
Run("SIGTRAN release gate evaluates artifact notes provenance and channel readiness", SigtranReleaseGateEvaluatesArtifactNotesProvenanceAndChannelReadiness);
Run("SIGTRAN release CI profile declares triggers secrets and plan", SigtranReleaseCiProfileDeclaresTriggersSecretsAndPlan);
Run("SIGTRAN release automation status summarizes foundation", SigtranReleaseAutomationStatusSummarizesReleaseAutomationFoundation);
Run("SIGTRAN developer experience catalog exposes adoption areas", SigtranDeveloperExperienceCatalogExposesAdoptionAreas);
Run("SIGTRAN M3UA quickstart exposes ordered ASP-to-SG steps", SigtranM3uaQuickstartExposesOrderedAspToSgSteps);
Run("SIGTRAN sample templates map sample ids to environments", SigtranSampleTemplatesMapSampleIdsToEnvironments);
Run("SIGTRAN configuration profiles separate development lab and production", SigtranConfigurationProfilesSeparateDevelopmentLabAndProduction);
Run("SIGTRAN troubleshooting index maps symptoms to next actions", SigtranTroubleshootingIndexMapsSymptomsToNextActions);
Run("SIGTRAN API reference index exposes onboarding APIs", SigtranApiReferenceIndexExposesOnboardingApis);
Run("SIGTRAN adoption gates separate developer readiness from enterprise production", SigtranAdoptionGatesSeparateDeveloperReadinessFromEnterpriseProduction);
Run("SIGTRAN documentation readiness reports developer docs gate", SigtranDocumentationReadinessReportsDeveloperDocsGate);
Run("SIGTRAN developer experience CI profile requires docs and adoption gates", SigtranDeveloperExperienceCiProfileRequiresDocsAndAdoptionGates);
Run("SIGTRAN developer experience status summarizes foundation", SigtranDeveloperExperienceStatusSummarizesDeveloperExperienceFoundation);
Run("SIGTRAN operations catalog exposes production support areas", SigtranOperationsCatalogExposesProductionSupportAreas);
Run("SIGTRAN runbook catalog exposes operational recovery paths", SigtranRunbookCatalogExposesOperationalRecoveryPaths);
Run("SIGTRAN incident response targets define severity timing", SigtranIncidentResponseTargetsDefineSeverityTiming);
Run("SIGTRAN health check matrix covers transport protocol evidence and release", SigtranHealthCheckMatrixCoversTransportProtocolEvidenceAndRelease);
Run("SIGTRAN rollback plan defines package recovery steps", SigtranRollbackPlanDefinesPackageRecoverySteps);
Run("SIGTRAN maintenance policy gates protocol and transport changes", SigtranMaintenancePolicyGatesProtocolAndTransportChanges);
Run("SIGTRAN support handbook defines public private and commercial channels", SigtranSupportHandbookDefinesPublicPrivateAndCommercialChannels);
Run("SIGTRAN operations readiness separates foundation from production", SigtranOperationsReadinessSeparatesFoundationFromProduction);
Run("SIGTRAN operations CI profile requires operations readiness", SigtranOperationsCiProfileRequiresOperationsReadiness);
Run("SIGTRAN operations status summarizes foundation", SigtranOperationsStatusSummarizesOperationsFoundation);
Run("SIGTRAN compliance catalog exposes enterprise audit areas", SigtranComplianceCatalogExposesEnterpriseAuditAreas);
Run("SIGTRAN audit event catalog marks evidence-bearing events", SigtranAuditEventCatalogMarksEvidenceBearingEvents);
Run("SIGTRAN evidence retention policy requires immutable redacted provenance", SigtranEvidenceRetentionPolicyRequiresImmutableRedactedProvenance);
Run("SIGTRAN license compliance policy tracks Apache and third-party obligations", SigtranLicenseCompliancePolicyTracksApacheAndThirdPartyObligations);
Run("SIGTRAN data handling rules classify confidential traces", SigtranDataHandlingRulesClassifyConfidentialTraces);
Run("SIGTRAN export control policy requires lawful operator authorization", SigtranExportControlPolicyRequiresLawfulOperatorAuthorization);
Run("SIGTRAN compliance readiness separates foundation from commercial claims", SigtranComplianceReadinessSeparatesFoundationFromCommercialClaims);
Run("SIGTRAN compliance CI profile requires compliance readiness", SigtranComplianceCiProfileRequiresComplianceReadiness);
Run("SIGTRAN compliance commercial gate waits for commercial readiness", SigtranComplianceCommercialGateWaitsForCommercialReadiness);
Run("SIGTRAN compliance status summarizes foundation", SigtranComplianceStatusSummarizesComplianceFoundation);
Run("SIGTRAN performance catalog exposes benchmark capacity and resource areas", SigtranPerformanceCatalogExposesBenchmarkCapacityAndResourceAreas);
Run("SIGTRAN benchmark scenarios include local and peer benchmarks", SigtranBenchmarkScenariosIncludeLocalAndPeerBenchmarks);
Run("SIGTRAN capacity profile describes enterprise load shape", SigtranCapacityProfileDescribesEnterpriseLoadShape);
Run("SIGTRAN throughput targets require benchmark evidence", SigtranThroughputTargetsRequireBenchmarkEvidence);
Run("SIGTRAN latency budgets define P95 and P99 bounds", SigtranLatencyBudgetsDefineP95AndP99Bounds);
Run("SIGTRAN load test plan defines warmup sustained and peak stages", SigtranLoadTestPlanDefinesWarmupSustainedAndPeakStages);
Run("SIGTRAN resource budget requires allocation tracking", SigtranResourceBudgetRequiresAllocationTracking);
Run("SIGTRAN performance readiness separates foundation from benchmark evidence", SigtranPerformanceReadinessSeparatesFoundationFromBenchmarkEvidence);
Run("SIGTRAN performance CI profile keeps benchmarks opt-in", SigtranPerformanceCiProfileKeepsBenchmarksOptIn);
Run("SIGTRAN performance status summarizes foundation", SigtranPerformanceStatusSummarizesPerformanceFoundation);
Run("SIGTRAN API surface catalog exposes protocol and governance surfaces", SigtranApiSurfaceCatalogExposesProtocolAndGovernanceSurfaces);
Run("SIGTRAN API stability contracts mark pre-stable surfaces", SigtranApiStabilityContractsMarkPreStableSurfaces);
Run("SIGTRAN API version matrix separates pre-stable and stable lines", SigtranApiVersionMatrixSeparatesPreStableAndStableLines);
Run("SIGTRAN deprecation policy requires obsolete migration and release notes", SigtranDeprecationPolicyRequiresObsoleteMigrationAndReleaseNotes);
Run("SIGTRAN migration guides require code samples", SigtranMigrationGuidesRequireCodeSamples);
Run("SIGTRAN breaking change review requires baseline migration and approval", SigtranBreakingChangeReviewRequiresBaselineMigrationAndApproval);
Run("SIGTRAN public API baseline covers known surfaces", SigtranPublicApiBaselineCoversKnownSurfaces);
Run("SIGTRAN API lifecycle readiness separates foundation from stable claims", SigtranApiLifecycleReadinessSeparatesFoundationFromStableClaims);
Run("SIGTRAN API lifecycle CI profile requires public API diff review", SigtranApiLifecycleCiProfileRequiresPublicApiDiffReview);
Run("SIGTRAN API lifecycle status summarizes foundation", SigtranApiLifecycleStatusSummarizesApiLifecycleFoundation);
Run("SIGTRAN configuration schema exposes required transport routing and security keys", SigtranConfigurationSchemaExposesRequiredTransportRoutingAndSecurityKeys);
Run("SIGTRAN configuration validation reports missing required keys", SigtranConfigurationValidationReportsMissingRequiredKeys);
Run("SIGTRAN environment matrix separates development lab and production requirements", SigtranEnvironmentMatrixSeparatesDevelopmentLabAndProductionRequirements);
Run("SIGTRAN secret policy rejects production plaintext secrets", SigtranSecretPolicyRejectsProductionPlaintextSecrets);
Run("SIGTRAN transport configuration requires PPID streams and reconnect policy", SigtranTransportConfigurationRequiresPpidStreamsAndReconnectPolicy);
Run("SIGTRAN routing configuration requires route validation and ambiguity rejection", SigtranRoutingConfigurationRequiresRouteValidationAndAmbiguityRejection);
Run("SIGTRAN configuration readiness separates foundation from commercial claims", SigtranConfigurationReadinessSeparatesFoundationFromCommercialClaims);
Run("SIGTRAN configuration CI profile rejects production plaintext secrets", SigtranConfigurationCiProfileRejectsProductionPlaintextSecrets);
Run("SIGTRAN configuration commercial gate waits for commercial readiness", SigtranConfigurationCommercialGateWaitsForCommercialReadiness);
Run("SIGTRAN configuration status summarizes foundation", SigtranConfigurationStatusSummarizesConfigurationFoundation);
Run("SIGTRAN native SCTP lab scenarios require Linux verification", SigtranNativeSctpLabScenariosRequireLinuxVerification);
Run("SIGTRAN native SCTP lab artifact manifest validates required artifacts", SigtranNativeSctpLabArtifactManifestValidatesRequiredArtifacts);
Run("SIGTRAN native SCTP lab run plan includes external peer traffic", SigtranNativeSctpLabRunPlanIncludesExternalPeerTraffic);
Run("SIGTRAN native SCTP lab commands require Linux and lksctp tools", SigtranNativeSctpLabCommandsRequireLinuxAndLksctpTools);
Run("SIGTRAN native SCTP lab run report identifies passing evidence", SigtranNativeSctpLabRunReportIdentifiesPassingEvidence);
Run("SIGTRAN native SCTP lab evidence registry requires all scenarios", SigtranNativeSctpLabEvidenceRegistryRequiresAllScenarios);
Run("SIGTRAN native SCTP lab readiness separates foundation from evidence", SigtranNativeSctpLabReadinessSeparatesFoundationFromEvidence);
Run("SIGTRAN native SCTP lab CI profile is opt-in and Linux-only", SigtranNativeSctpLabCiProfileIsOptInAndLinuxOnly);
Run("SIGTRAN native SCTP lab commercial gate waits for complete evidence", SigtranNativeSctpLabCommercialGateWaitsForCompleteEvidence);
Run("SIGTRAN native SCTP lab verification status summarizes foundation", SigtranNativeSctpLabVerificationStatusSummarizesFoundation);
Run("SIGTRAN external peer interop environment requires Linux SCTP peer and packet capture", SigtranExternalPeerInteropEnvironmentRequiresLinuxSctpPeerAndPacketCapture);
Run("SIGTRAN external peer interop configuration exposes ASP-to-SG defaults", SigtranExternalPeerInteropConfigurationExposesAspToSgDefaults);
Run("SIGTRAN external peer trace expectations cover ASP lifecycle and DATA", SigtranExternalPeerTraceExpectationsCoverAspLifecycleAndData);
Run("SIGTRAN external peer artifact manifest requires trace peer config logs and comparison", SigtranExternalPeerArtifactManifestRequiresTracePeerConfigLogsAndComparison);
Run("SIGTRAN external peer run plan is executable with default contracts", SigtranExternalPeerRunPlanIsExecutableWithDefaultContracts);
Run("SIGTRAN external peer commands require peer and packet capture", SigtranExternalPeerCommandsRequirePeerAndPacketCapture);
Run("SIGTRAN external peer run report identifies passing evidence", SigtranExternalPeerRunReportIdentifiesPassingEvidence);
Run("SIGTRAN external peer evidence registry starts empty", SigtranExternalPeerEvidenceRegistryStartsEmpty);
Run("SIGTRAN external peer readiness separates foundation from evidence", SigtranExternalPeerReadinessSeparatesFoundationFromEvidence);
Run("SIGTRAN external peer commercial readiness aggregates selection lab and evidence", SigtranExternalPeerCommercialReadinessAggregatesSelectionLabAndEvidence);
Run("SIGTRAN external peer status summarizes execution foundation", SigtranExternalPeerStatusSummarizesExecutionFoundation);
Run("SIGTRAN commercial roadmap realignment status summarizes package-neutral completion", SigtranCommercialRoadmapRealignmentStatusSummarizesPackageNeutralCompletion);
Run("SIGTRAN protocol interop vector catalog covers SCCP TCAP and MAP", SigtranProtocolInteropVectorCatalogCoversSccpTcapAndMap);
Run("SIGTRAN protocol interop references require trace validation", SigtranProtocolInteropReferencesRequireTraceValidation);
Run("SIGTRAN protocol interop artifact manifest requires reference SDK and comparison", SigtranProtocolInteropArtifactManifestRequiresReferenceSdkAndComparison);
Run("SIGTRAN protocol interop comparison rules are commercial validation ready", SigtranProtocolInteropComparisonRulesAreCommercialValidationReady);
Run("SIGTRAN protocol interop run plan is executable with external vectors", SigtranProtocolInteropRunPlanIsExecutableWithExternalVectors);
Run("SIGTRAN protocol interop commands require vector root and reports", SigtranProtocolInteropCommandsRequireVectorRootAndReports);
Run("SIGTRAN protocol interop run report identifies passing evidence", SigtranProtocolInteropRunReportIdentifiesPassingEvidence);
Run("SIGTRAN protocol interop evidence registry starts empty", SigtranProtocolInteropEvidenceRegistryStartsEmpty);
Run("SIGTRAN protocol interop readiness separates foundation from evidence", SigtranProtocolInteropReadinessSeparatesFoundationFromEvidence);
Run("SIGTRAN protocol interop status summarizes vector foundation", SigtranProtocolInteropStatusSummarizesVectorFoundation);
Run("SIGTRAN commercial evidence requirements cover production claim areas", SigtranCommercialEvidenceRequirementsCoverProductionClaimAreas);
Run("SIGTRAN commercial evidence manifest satisfies requirements with digests", SigtranCommercialEvidenceManifestSatisfiesRequirementsWithDigests);
Run("SIGTRAN commercial evidence bundle keeps empty dossier incomplete", SigtranCommercialEvidenceBundleKeepsEmptyDossierIncomplete);
Run("SIGTRAN commercial evidence bundle completes with retained artifacts", SigtranCommercialEvidenceBundleCompletesWithRetainedArtifacts);
Run("SIGTRAN commercial evidence gate reports missing current evidence", SigtranCommercialEvidenceGateReportsMissingCurrentEvidence);
Run("SIGTRAN commercial evidence gate allows complete verified dossier", SigtranCommercialEvidenceGateAllowsCompleteVerifiedDossier);
Run("SIGTRAN commercial evidence readiness separates foundation from claims", SigtranCommercialEvidenceReadinessSeparatesFoundationFromClaims);
Run("SIGTRAN commercial evidence CI profile requires retained bundle", SigtranCommercialEvidenceCiProfileRequiresRetainedBundle);
Run("SIGTRAN commercial evidence status summarizes dossier foundation", SigtranCommercialEvidenceStatusSummarizesDossierFoundation);
Run("SIGTRAN supply chain automation plan wires SBOM signing provenance and evidence", SigtranSupplyChainAutomationPlanWiresSbomSigningProvenanceAndEvidence);
Run("SIGTRAN supply chain commands expose ordered release security steps", SigtranSupplyChainCommandsExposeOrderedReleaseSecuritySteps);
Run("SIGTRAN supply chain artifact manifest validates required artifacts", SigtranSupplyChainArtifactManifestValidatesRequiredArtifacts);
Run("SIGTRAN supply chain gate blocks incomplete promotion evidence", SigtranSupplyChainGateBlocksIncompletePromotionEvidence);
Run("SIGTRAN supply chain gate allows complete verified promotion", SigtranSupplyChainGateAllowsCompleteVerifiedPromotion);
Run("SIGTRAN supply chain readiness separates foundation from promotion", SigtranSupplyChainReadinessSeparatesFoundationFromPromotion);
Run("SIGTRAN supply chain CI profile requires signing secrets", SigtranSupplyChainCiProfileRequiresSigningSecrets);
Run("SIGTRAN supply chain status summarizes automation foundation", SigtranSupplyChainStatusSummarizesAutomationFoundation);
Run("SIGTRAN supply chain references align with SBOM and signing plans", SigtranSupplyChainReferencesAlignWithSbomAndSigningPlans);
Run("SIGTRAN supply chain artifact digests are mandatory for promotion", SigtranSupplyChainArtifactDigestsAreMandatoryForPromotion);
Run("SIGTRAN release workflow plan includes release triggers and stages", SigtranReleaseWorkflowPlanIncludesReleaseTriggersAndStages);
Run("SIGTRAN release workflow requires supply chain evidence and publish secrets", SigtranReleaseWorkflowRequiresSupplyChainEvidenceAndPublishSecrets);
Run("SIGTRAN release workflow readiness reports concrete workflow file", SigtranReleaseWorkflowReadinessReportsConcreteWorkflowFile);
Run("SIGTRAN release workflow status summarizes orchestration foundation", SigtranReleaseWorkflowStatusSummarizesOrchestrationFoundation);
Run("SIGTRAN release workflow file contract tracks concrete workflow file", SigtranReleaseWorkflowFileContractTracksConcreteWorkflowFile);
Run("SIGTRAN release workflow validation accepts concrete workflow YAML", SigtranReleaseWorkflowValidationAcceptsConcreteWorkflowYaml);
Run("SIGTRAN release publish guard blocks accidental publication", SigtranReleasePublishGuardBlocksAccidentalPublication);
Run("SIGTRAN release publish guard allows intentional tagged publication", SigtranReleasePublishGuardAllowsIntentionalTaggedPublication);
Run("SIGTRAN release workflow artifact rules retain packages and evidence", SigtranReleaseWorkflowArtifactRulesRetainPackagesAndEvidence);
Run("SIGTRAN release workflow permissions use least privilege", SigtranReleaseWorkflowPermissionsUseLeastPrivilege);
Run("SIGTRAN release workflow concurrency prevents overlapping releases", SigtranReleaseWorkflowConcurrencyPreventsOverlappingReleases);
Run("SIGTRAN release workflow environment exposes supply chain and evidence variables", SigtranReleaseWorkflowEnvironmentExposesSupplyChainAndEvidenceVariables);
Run("SIGTRAN release promotion gate blocks incomplete release evidence", SigtranReleasePromotionGateBlocksIncompleteReleaseEvidence);
Run("SIGTRAN release promotion gate allows complete release evidence", SigtranReleasePromotionGateAllowsCompleteReleaseEvidence);
Run("SIGTRAN release version policy accepts SemVer tags", SigtranReleaseVersionPolicyAcceptsSemVerTags);
Run("SIGTRAN NuGet metadata contract matches project file", SigtranNuGetMetadataContractMatchesProjectFile);
Run("SIGTRAN package layout exposes nupkg and symbols", SigtranPackageLayoutExposesNupkgAndSymbols);
Run("SIGTRAN NuGet publish plans separate dry-run and publish", SigtranNuGetPublishPlansSeparateDryRunAndPublish);
Run("SIGTRAN publication credential policy requires commercial secrets", SigtranPublicationCredentialPolicyRequiresCommercialSecrets);
Run("SIGTRAN publication channel policy separates prerelease and stable", SigtranPublicationChannelPolicySeparatesPrereleaseAndStable);
Run("SIGTRAN package integrity manifest requires package digests", SigtranPackageIntegrityManifestRequiresPackageDigests);
Run("SIGTRAN publication evidence manifest requires integrity and release evidence", SigtranPublicationEvidenceManifestRequiresIntegrityAndReleaseEvidence);
Run("SIGTRAN publication gate blocks incomplete publish readiness", SigtranPublicationGateBlocksIncompletePublishReadiness);
Run("SIGTRAN publication gate allows complete publish readiness", SigtranPublicationGateAllowsCompletePublishReadiness);
Run("SIGTRAN package publication status summarizes readiness foundation", SigtranPackagePublicationStatusSummarizesReadinessFoundation);
Run("SIGTRAN commercial release execution evidence tracks passed and blocked artifacts", SigtranCommercialReleaseExecutionEvidenceTracksPassedAndBlockedArtifacts);
Run("SIGTRAN Linux SCTP evidence records passing smoke capture", SigtranLinuxSctpEvidenceRecordsPassingSmokeCapture);
Run("SIGTRAN external peer interop blocker evidence records retained failure context", SigtranExternalPeerInteropBlockerEvidenceRecordsRetainedFailureContext);
Run("SIGTRAN commercial release artifact dossier tracks retained and missing artifacts", SigtranCommercialReleaseArtifactDossierTracksRetainedAndMissingArtifacts);
Run("SIGTRAN SBOM execution evidence records generated SPDX output", SigtranSbomExecutionEvidenceRecordsGeneratedSpdxOutput);
Run("SIGTRAN package signing execution evidence records verification blocker", SigtranPackageSigningExecutionEvidenceRecordsVerificationBlocker);
Run("SIGTRAN provenance execution evidence records package and SBOM digests", SigtranProvenanceExecutionEvidenceRecordsPackageAndSbomDigests);
Run("SIGTRAN benchmark execution evidence records smoke workload limits", SigtranBenchmarkExecutionEvidenceRecordsSmokeWorkloadLimits);
Run("SIGTRAN public API baseline evidence records generated member baseline", SigtranPublicApiBaselineEvidenceRecordsGeneratedMemberBaseline);
Run("SIGTRAN commercial release execution readiness reports remaining blockers", SigtranCommercialReleaseExecutionReadinessReportsRemainingBlockers);
Run("SIGTRAN status capabilities use domain documentation labels", SigtranStatusCapabilitiesUseDomainDocumentationLabels);
Run("Native SCTP platform probe reports socket creation capability", NativeSctpPlatformProbeReportsSocketCreationCapability);
Run("Native SCTP socket factory creates or reports unsupported platform", NativeSctpSocketFactoryCreatesOrReportsUnsupportedPlatform);
Run("Native SCTP connection planner resolves endpoints", NativeSctpConnectionPlannerResolvesEndpoints);
Run("Native SCTP socket adapter reports lifecycle health", NativeSctpSocketAdapterReportsLifecycleHealth);
Run("Native SCTP connector reports unsupported platform safely", NativeSctpConnectorReportsUnsupportedPlatformSafely);
Run("Native SCTP listener validates options and unsupported platform", NativeSctpListenerValidatesOptionsAndUnsupportedPlatform);
Run("Native SCTP lab profile is opt-in", NativeSctpLabProfileIsOptIn);
Run("Native SCTP readiness reports foundation and verification gates", NativeSctpReadinessReportsFoundationAndVerificationGates);
Run("SIGTRAN native SCTP implementation status summarizes foundation", SigtranNativeSctpImplementationStatusSummarizesNativeSctpFoundation);
Run("TCAP BER element encodes short and long lengths", TcapBerElementEncodesShortAndLongLengths);
Run("TCAP transaction identifiers use BER context tags", TcapTransactionIdentifiersUseBerContextTags);
Run("TCAP BER Invoke component round-trips", TcapBerInvokeComponentRoundTrips);
Run("TCAP BER outcome components round-trip", TcapBerOutcomeComponentsRoundTrip);
Run("TCAP transaction message wraps component portion", TcapTransactionMessageWrapsComponentPortion);
Run("TCAP dialogue portion carries application context", TcapDialoguePortionCarriesApplicationContext);
Run("TCAP dialogue controller tracks state and invoke timeouts", TcapDialogueControllerTracksStateAndInvokeTimeouts);
Run("TCAP allocators issue transaction and invoke identifiers", TcapAllocatorsIssueTransactionAndInvokeIdentifiers);
Run("TCAP session builder creates Begin and End messages", TcapSessionBuilderCreatesBeginAndEndMessages);
Run("TCAP readiness reports foundation status", TcapReadinessReportsFoundationStatus);
Run("MAP SMS operation catalog and parameter set encode BER", MapSmsOperationCatalogAndParameterSetEncodeBer);
Run("MAP SMS address primitives encode TBCD digits", MapSmsAddressPrimitivesEncodeTbcdDigits);
Run("MAP MO-ForwardSM model encodes required parameters", MapMoForwardSmModelEncodesRequiredParameters);
Run("MAP MT-ForwardSM model encodes required parameters", MapMtForwardSmModelEncodesRequiredParameters);
Run("MAP SendRoutingInfoForSM model encodes routing parameters", MapSendRoutingInfoForSmModelEncodesRoutingParameters);
Run("MAP ReportSM-DeliveryStatus model encodes delivery status", MapReportSmDeliveryStatusModelEncodesDeliveryStatus);
Run("MAP AlertServiceCentre model encodes alert parameters", MapAlertServiceCentreModelEncodesAlertParameters);
Run("MAP SMS error mapper and extension container encode values", MapSmsErrorMapperAndExtensionContainerEncodeValues);
Run("MAP SMS TCAP client builds Begin Invoke transactions", MapSmsTcapClientBuildsBeginInvokeTransactions);
Run("MAP SMS readiness reports foundation status", MapSmsReadinessReportsFoundationStatus);
Run("MTP3 routing label and SIO round-trip", Mtp3RoutingLabelAndSioRoundTrip);
Run("SCCP protocol constants expose connectionless classes", SccpProtocolConstantsExposeConnectionlessClasses);
Run("SCCP party address encodes SSN and global title", SccpPartyAddressEncodesSsnAndGlobalTitle);
Run("SCCP UDT codec uses variable parameter pointers", SccpUdtCodecUsesVariableParameterPointers);
Run("SCCP XUDT codec preserves hop counter", SccpXudtCodecPreservesHopCounter);
Run("SCCP segmentation parameter round-trips", SccpSegmentationParameterRoundTrips);
Run("SCCP XUDT carries segmentation optional parameter", SccpXudtCarriesSegmentationOptionalParameter);
Run("SCCP LUDT codec carries long user data", SccpLudtCodecCarriesLongUserData);
Run("SCCP UDTS codec carries return cause", SccpUdtsCodecCarriesReturnCause);
Run("SCCP route table resolves SSN and global title routes", SccpRouteTableResolvesSsnAndGlobalTitleRoutes);
Run("SCCP readiness reports foundation status", SccpReadinessReportsFoundationStatus);
Run("SCTP payload metadata stores stream and PPID values", SctpPayloadMetadataStoresStreamAndPpidValues);
Run("SCTP association events describe lifecycle state", SctpAssociationEventsDescribeLifecycleState);
Run("SCTP connection options validate endpoints and stream counts", SctpConnectionOptionsValidateEndpointsAndStreamCounts);
Run("SCTP PPID helpers recognize SIGTRAN payload identifiers", SctpPpidHelpersRecognizeSigtranPayloadIdentifiers);
Run("SCTP stream selection policies choose outbound streams", SctpStreamSelectionPoliciesChooseOutboundStreams);
Run("SCTP reconnect policies compute bounded delays", SctpReconnectPoliciesComputeBoundedDelays);
Run("SCTP transport health snapshots expose association details", SctpTransportHealthSnapshotsExposeAssociationDetails);
Run("TCP SCTP adapter exposes development metadata and health", TcpSctpAdapterExposesDevelopmentMetadataAndHealth);
Run("SCTP transport readiness reports foundation status", SctpTransportReadinessReportsFoundationStatus);
Run("M3UA Payload Data uses network byte order and RFC-style TLV length", M3uaPayloadDataUsesNetworkOrder);
Run("M3UA protocol exposes public metadata", M3uaProtocolExposesPublicMetadata);
Run("M3UA alpha readiness report describes release gate", M3uaAlphaReadinessReportDescribesReleaseGate);
Run("M3UA decoder returns the complete Protocol Data value", M3uaDecoderReturnsProtocolDataValue);
Run("M3UA parses Payload Data optional fields", M3uaParsesPayloadDataOptionalFields);
Run("M3UA rejects Payload Data without Protocol Data", M3uaRejectsPayloadDataWithoutProtocolData);
Run("M3UA reports supported typed message kinds", M3uaReportsSupportedTypedMessageKinds);
Run("M3UA dispatches known typed messages", M3uaDispatchesKnownTypedMessages);
Run("M3UA dispatcher rejects unsupported message types", M3uaDispatcherRejectsUnsupportedMessageTypes);
Run("M3UA route table resolves the most specific DATA route", M3uaRouteTableResolvesMostSpecificDataRoute);
Run("M3UA route table rejects ambiguous DATA routes", M3uaRouteTableRejectsAmbiguousDataRoutes);
Run("M3UA route table rejects duplicate selectors", M3uaRouteTableRejectsDuplicateSelectors);
Run("M3UA route table removes and clears routes", M3uaRouteTableRemovesAndClearsRoutes);
Run("M3UA route table snapshots and finds routes by name", M3uaRouteTableSnapshotsAndFindsRoutesByName);
Run("M3UA route table replaces routes by selector", M3uaRouteTableReplacesRoutesBySelector);
Run("M3UA route table adds or replaces routes by selector", M3uaRouteTableAddsOrReplacesRoutesBySelector);
Run("M3UA inbound processor updates ASP state and routes DATA", M3uaInboundProcessorUpdatesAspStateAndRoutesData);
Run("M3UA inbound processor can require active ASP for DATA", M3uaInboundProcessorCanRequireActiveAspForData);
Run("M3UA inbound processor rejects unrouted DATA when routes exist", M3uaInboundProcessorRejectsUnroutedDataWhenRoutesExist);
Run("M3UA outbound processor applies defaults to DATA", M3uaOutboundProcessorAppliesDefaultsToData);
Run("M3UA outbound processor can require active ASP for DATA", M3uaOutboundProcessorCanRequireActiveAspForData);
Run("M3UA outbound processor builds ASP Active with default Routing Context", M3uaOutboundProcessorBuildsAspActiveWithDefaultRoutingContext);
Run("M3UA transport session sends outbound DATA", M3uaTransportSessionSendsOutboundData);
Run("M3UA transport session receives inbound DATA", M3uaTransportSessionReceivesInboundData);
Run("M3UA transport session waits for typed messages", M3uaTransportSessionWaitsForTypedMessages);
Run("M3UA transport session waits for ASP transitions", M3uaTransportSessionWaitsForAspTransitions);
Run("M3UA transport session disposes owned socket", M3uaTransportSessionDisposesOwnedSocket);
Run("M3UA transport session tracks counters", M3uaTransportSessionTracksCounters);
Run("M3UA transport session resets counters", M3uaTransportSessionResetsCounters);
Run("M3UA transport session notifies ASP transport loss", M3uaTransportSessionNotifiesAspTransportLoss);
Run("M3UA diagnostics format hex and summaries", M3uaDiagnosticsFormatHexAndSummaries);
Run("M3UA ASP client completes startup handshake", M3uaAspClientCompletesStartupHandshake);
Run("M3UA ASP startup options validate and describe settings", M3uaAspStartupOptionsValidateAndDescribeSettings);
Run("M3UA ASP client resets before startup handshake", M3uaAspClientResetsBeforeStartupHandshake);
Run("M3UA ASP client fails when acknowledgement is missing", M3uaAspClientFailsWhenAcknowledgementIsMissing);
Run("M3UA transport session sends Heartbeat", M3uaTransportSessionSendsHeartbeat);
Run("M3UA transport session acknowledges inbound Heartbeat", M3uaTransportSessionAcknowledgesInboundHeartbeat);
Run("M3UA ASP client sends Heartbeat and waits for Ack", M3uaAspClientSendsHeartbeatAndWaitsForAck);
Run("M3UA ASP client deactivates and stops", M3uaAspClientDeactivatesAndStops);
Run("M3UA parameter reader skips padding between TLVs", M3uaParameterReaderSkipsPadding);
Run("M3UA builds ASP Up with ASP Identifier and Info String", M3uaBuildsAspUp);
Run("M3UA builds Heartbeat Ack with unchanged heartbeat data", M3uaBuildsHeartbeatAck);
Run("M3UA builds ASP Active with Traffic Mode and Routing Context", M3uaBuildsAspActive);
Run("M3UA builds ASP Inactive Ack with Routing Context", M3uaBuildsAspInactiveAck);
Run("M3UA parses ASP Up into a typed ASPSM message", M3uaParsesAspUp);
Run("M3UA parses ASP Active into a typed ASPTM message", M3uaParsesAspActive);
Run("M3UA rejects malformed typed Routing Context", M3uaRejectsMalformedTypedRoutingContext);
Run("M3UA ASP state machine follows the active lifecycle", M3uaAspStateMachineFollowsActiveLifecycle);
Run("M3UA ASP state machine rejects invalid transitions", M3uaAspStateMachineRejectsInvalidTransitions);
Run("M3UA ASP session applies acknowledgement lifecycle messages", M3uaAspSessionAppliesAcknowledgementLifecycle);
Run("M3UA ASP session resets negotiated state", M3uaAspSessionResetsNegotiatedState);
Run("M3UA ASP session rejects acknowledgement messages in the wrong state", M3uaAspSessionRejectsWrongStateAcknowledgement);
Run("M3UA parses Management Error messages", M3uaParsesManagementError);
Run("M3UA parses Management Notify messages", M3uaParsesManagementNotify);
Run("M3UA transport session sends Management messages", M3uaTransportSessionSendsManagementMessages);
Run("M3UA rejects invalid Management Notify status information", M3uaRejectsInvalidManagementNotifyStatusInformation);
Run("M3UA parses SSNM Destination Unavailable messages", M3uaParsesSsnmDestinationUnavailable);
Run("M3UA rejects SSNM messages without Affected Point Code", M3uaRejectsSsnmWithoutAffectedPointCode);
Run("M3UA parses SSNM Destination User Part Unavailable messages", M3uaParsesDestinationUserPartUnavailable);
Run("M3UA rejects DUPU with non-zero affected point-code mask", M3uaRejectsDupuWithNonZeroMask);
Run("M3UA parses SSNM Signalling Congestion messages", M3uaParsesSignallingCongestion);
Run("M3UA transport session sends SSNM messages", M3uaTransportSessionSendsSsnmMessages);
Run("M3UA rejects SCON without Affected Point Code", M3uaRejectsSconWithoutAffectedPointCode);
Run("M3UA parses RKM Registration Request messages", M3uaParsesRegistrationRequest);
Run("M3UA parses RKM Registration Response messages", M3uaParsesRegistrationResponse);
Run("M3UA parses RKM Deregistration messages", M3uaParsesDeregistrationMessages);
Run("M3UA exposes RKM response convenience helpers", M3uaExposesRkmResponseConvenienceHelpers);
Run("M3UA RKM client registers and deregisters routing keys", M3uaRkmClientRegistersAndDeregistersRoutingKeys);
Run("M3UA RKM client can require successful responses", M3uaRkmClientRequiresSuccessfulResponses);
Run("M3UA rejects Routing Key without Destination Point Code", M3uaRejectsRoutingKeyWithoutDestinationPointCode);

static void SigtranTraceFormatterEmitsSummariesAndHexDumps()
{
    SigtranTraceFrame frame = new(
        new DateTimeOffset(2026, 6, 19, 10, 15, 0, TimeSpan.Zero),
        "M3UA",
        SigtranTraceDirection.Outbound,
        "asp:2905",
        "sg:2905",
        new byte[] { 0x01, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });

    string summary = SigtranTraceFormatter.FormatSummary(frame);
    Assert(summary.Contains("M3UA asp:2905 -> sg:2905 bytes=8", StringComparison.Ordinal), summary);

    string dump = SigtranTraceFormatter.FormatHexDump(frame);
    Assert(dump.Contains("0000: 01 00 01 01 00 00 00 08", StringComparison.Ordinal), dump);
}

static void SigtranConformanceRegistryStoresVectorsDeterministically()
{
    SigtranConformanceRegistry registry = new();
    registry.Add(new SigtranConformanceVector("m3ua/aspup", "M3UA", "ASP Up", new byte[] { 0x01 }, "internal"));
    registry.Add(new SigtranConformanceVector("map/mo", "MAP", "MO ForwardSM", new byte[] { 0x02 }, "internal"));

    Assert(registry.TryGet("m3ua/aspup", out SigtranConformanceVector? vector), "conformance vector should be found");
    AssertEqual("M3UA", vector!.Protocol, "conformance vector protocol");
    Assert(vector.Describe().Contains("bytes=1", StringComparison.Ordinal), vector.Describe());
    AssertEqual("m3ua/aspup", registry.Snapshot()[0].Id, "conformance registry deterministic order");
    AssertThrows<InvalidOperationException>(() => registry.Add(vector));
}

static void SigtranBuiltInVectorsIncludeM3uaAndMapPayloads()
{
    SigtranConformanceRegistry registry = SigtranBuiltInVectors.CreateRegistry();
    AssertEqual(2, registry.Snapshot().Count, "built-in vector count");
    Assert(registry.TryGet("m3ua/aspsm/asp-up-basic", out SigtranConformanceVector? aspUp), "M3UA ASP Up vector should exist");
    AssertEqual("M3UA", aspUp!.Protocol, "M3UA vector protocol");
    Assert(aspUp.Payload.Length >= 8, "M3UA vector should contain a full message header");
    Assert(registry.TryGet("map/sms/mo-forward-sm-basic", out SigtranConformanceVector? mo), "MAP MO vector should exist");
    AssertEqual("MAP", mo!.Protocol, "MAP vector protocol");
}

static void SigtranSimulatorScriptEmitsTraceSummaries()
{
    SigtranSimulatorEndpoint asp = new("asp", SigtranSimulatorRole.Asp);
    SigtranSimulatorEndpoint sg = new("sg", SigtranSimulatorRole.SignallingGateway);
    SigtranSimulatorScript script = new();

    script.Add(new SigtranSimulatorStep(asp, sg, "M3UA", new byte[] { 0x01, 0x00, 0x03, 0x01 }, "ASP Up"));

    IReadOnlyList<string> summaries = script.FormatTraceSummaries(new DateTimeOffset(2026, 6, 19, 10, 0, 0, TimeSpan.Zero));

    AssertEqual(1, script.Snapshot().Count, "simulator step count");
    Assert(summaries[0].Contains("M3UA asp -> sg bytes=4", StringComparison.Ordinal), summaries[0]);
    AssertEqual(SigtranSimulatorRole.Asp, asp.Role, "simulator ASP role");
}

static void MapSmsSimulatorFlowBuildsTcapBackedScript()
{
    MapSmsAddress subscriber = new(MapSmsAddressKind.Msisdn, "989121234567");
    MapSmsAddress imsi = new(MapSmsAddressKind.Imsi, "432101234567890");
    MapSmsAddress serviceCentre = new(MapSmsAddressKind.ServiceCentre, "989120000000");

    SigtranSimulatorScript script = new MapSmsSimulatorFlowBuilder()
        .AddSendRoutingInfoForShortMessage(subscriber, serviceCentre, gprsSupportIndicator: true)
        .AddMobileTerminatedForwardShortMessage(imsi, serviceCentre, [0x11, 0x22])
        .AddReportShortMessageDeliveryStatus(subscriber, serviceCentre, MapSmsDeliveryStatus.Delivered)
        .Build();

    IReadOnlyList<SigtranSimulatorStep> steps = script.Snapshot();
    IReadOnlyList<string> summaries = script.FormatTraceSummaries(DateTimeOffset.UnixEpoch);

    AssertEqual(3, steps.Count, "MAP SMS simulator step count");
    AssertEqual("TCAP/MAP", steps[0].Protocol, "MAP SMS simulator protocol");
    Assert(summaries[0].Contains("smsc -> hlr", StringComparison.Ordinal), summaries[0]);
    Assert(summaries[1].Contains("smsc -> msc", StringComparison.Ordinal), summaries[1]);
    Assert(summaries[2].Contains("hlr -> smsc", StringComparison.Ordinal), summaries[2]);
    Assert(steps[0].Payload.Length > 20, "MAP SMS simulator payload should contain encoded TCAP");
}

static void SigtranLocalTcpSampleDescribesM3uaTransport()
{
    SigtranLocalTcpScenario scenario = SigtranTransportSamples.CreateLocalM3uaAspToSg(2905);
    SctpConnectionOptions options = scenario.ToClientConnectionOptions();

    AssertEqual("local-m3ua-asp-to-sg", scenario.Name, "local TCP scenario name");
    AssertEqual("sg", scenario.Server.Name, "local TCP server name");
    AssertEqual((uint)SctpPayloadProtocolIdentifiers.M3ua, scenario.Metadata.PayloadProtocolIdentifier, "local TCP scenario PPID");
    AssertEqual("127.0.0.1", options.RemoteEndpoint.Host, "local TCP remote host");
    AssertEqual(2905, options.RemoteEndpoint.Port, "local TCP remote port");
    Assert(scenario.Describe().Contains("asp@127.0.0.1:2906 -> sg@127.0.0.1:2905", StringComparison.Ordinal), scenario.Describe());
    AssertThrows<ArgumentOutOfRangeException>(() => SigtranTransportSamples.CreateLocalM3uaAspToSg(65535));
}

static void SigtranSampleCatalogExposesSupportedScenarios()
{
    IReadOnlyList<SigtranSampleDescriptor> samples = SigtranSampleCatalog.GetSamples();

    AssertEqual(4, samples.Count, "sample catalog count");
    AssertEqual("m3ua-asp-to-sg", samples[0].Id, "sample catalog deterministic first id");
    Assert(SigtranSampleCatalog.TryGet("SCCP-MAP-SMS", out SigtranSampleDescriptor? mapSample), "SCCP/MAP sample should be discoverable");
    AssertEqual(SigtranSampleKind.SccpMapSms, mapSample!.Kind, "SCCP/MAP sample kind");
    Assert(mapSample.Describe().Contains("SCCP, TCAP, MAP", StringComparison.Ordinal), mapSample.Describe());
    Assert(!SigtranSampleCatalog.TryGet("missing", out _), "missing sample should not be found");
}

static void SigtranCiVerificationProfileExposesOfficialCommands()
{
    SigtranCiVerificationProfile profile = SigtranCiVerification.CreateDefaultProfile();
    IReadOnlyList<string> commands = profile.GetCommands();

    AssertEqual("10.0.x", profile.DotNetVersion, "CI profile .NET version");
    AssertEqual(3, profile.Steps.Count, "CI profile step count");
    Assert(commands[0].Contains("dotnet build", StringComparison.Ordinal), commands[0]);
    Assert(commands[1].Contains("Sigtran.NET.Tests", StringComparison.Ordinal), commands[1]);
    Assert(commands[2].Contains("dotnet pack", StringComparison.Ordinal), commands[2]);
}

static void SigtranInteroperabilityReadinessReportsFoundationStatus()
{
    SigtranInteroperabilityReadinessReport report = SigtranInteroperabilityReadiness.GetReport();

    Assert(report.FoundationReady, "interoperability tooling foundation should be ready");
    Assert(!report.IsProductionReady, "interoperability tooling should wait for external lab evidence");
    AssertEqual(8, report.FoundationCapabilityCount, "interoperability foundation capability count");
    AssertEqual(SigtranInteroperabilityReadiness.RequiredFoundationCapabilityCount, report.FoundationCapabilityCount, "interoperability required capability count");
    Assert(report.Describe().Contains("externalLab=False", StringComparison.Ordinal), report.Describe());
}

static void SigtranInteroperabilityToolingStatusSummarizesCompletedTooling()
{
    IReadOnlyList<string> capabilities = SigtranInteroperabilityToolingStatus.GetCompletedCapabilities();

    AssertEqual("Interoperability and Tooling", SigtranInteroperabilityToolingStatus.StatusLabel, "interoperability tooling label");
    AssertEqual(10, SigtranInteroperabilityToolingStatus.CompletedUnitCount, "interoperability tooling completed unit count");
    AssertEqual(10, capabilities.Count, "interoperability tooling capability count");
    Assert(capabilities.Contains("interoperability-readiness-report"), "interoperability tooling status should include readiness report");
    Assert(SigtranInteroperabilityToolingStatus.Describe().Contains("foundationReady=True", StringComparison.Ordinal), SigtranInteroperabilityToolingStatus.Describe());
}

static void SigtranCommercialReadinessReportsReleaseGates()
{
    SigtranCommercialReadinessReport report = SigtranCommercialReadiness.GetReport();

    Assert(report.InternalReleaseReady, "commercial readiness should see internal release foundation");
    Assert(!report.CommercialReady, "commercial readiness should wait for external gates");
    Assert(report.Describe().Contains("nativeSctp=False", StringComparison.Ordinal), report.Describe());
    Assert(report.Describe().Contains("externalInterop=False", StringComparison.Ordinal), report.Describe());
}

static void SigtranNativeSctpSupportMatrixReportsVerificationStatus()
{
    IReadOnlyList<SigtranNativeSctpSupportEntry> matrix = SigtranNativeSctpSupport.GetSupportMatrix();

    AssertEqual(3, matrix.Count, "native SCTP support matrix count");
    AssertEqual(SigtranOperatingSystemFamily.Linux, matrix[0].OperatingSystem, "native SCTP Linux row");
    AssertEqual(SigtranNativeSctpSupportStatus.VerificationRequired, matrix[0].Status, "native SCTP Linux status");
    AssertEqual(SigtranNativeSctpSupportStatus.ContractOnly, matrix[1].Status, "native SCTP Windows status");
    Assert(SigtranNativeSctpSupport.IsImplementationFoundationReady(), "native SCTP implementation foundation should be ready");
    Assert(!SigtranNativeSctpSupport.IsProductionVerified(), "native SCTP should not be production verified yet");
}

static void SigtranInteropEvidenceRegistryTracksLabResults()
{
    SigtranInteropEvidenceRegistry registry = new();
    registry.Add(new SigtranInteropEvidenceItem("lab/linux/m3ua-asp", "external-sigtran-peer", "M3UA ASP to SG", "traces/m3ua-asp.pcapng", SigtranInteropEvidenceResult.Passed));

    AssertEqual(1, registry.Snapshot().Count, "interop evidence count");
    Assert(registry.HasPassingEvidence(), "interop evidence should pass with one passing item");
    AssertThrows<InvalidOperationException>(() => registry.Add(registry.Snapshot()[0]));
    Assert(!SigtranInteropEvidence.CreateCurrentRegistry().HasPassingEvidence(), "current interop evidence should be empty until real lab artifacts are added");
}

static void SigtranReleaseCandidateManifestReportsPromotionGates()
{
    SigtranReleaseCandidateManifest manifest = SigtranReleaseCandidate.Create("1.0.0-alpha.1", "abcdef0");

    AssertEqual("Sigtran.NET", manifest.PackageId, "release candidate package id");
    Assert(manifest.CanPublishReleaseCandidate, "release candidate should be publishable after internal gates");
    Assert(!manifest.CanPromoteToCommercialProduction, "release candidate should not promote without commercial gates");
    Assert(manifest.Describe().Contains("commercialProduction=False", StringComparison.Ordinal), manifest.Describe());
}

static void SigtranPackageGovernanceReportsCommercialRequirements()
{
    SigtranPackageGovernancePolicy current = SigtranPackageGovernance.CreateCurrentPolicy();
    SigtranPackageGovernancePolicy commercial = SigtranPackageGovernance.CreateCommercialTargetPolicy();

    Assert(current.IsSatisfiedByCurrentPackage, "current package governance should reflect existing package metadata");
    Assert(!commercial.IsSatisfiedByCurrentPackage, "commercial governance should wait for signing and SBOM");
    Assert(commercial.Describe().Contains("signing=True", StringComparison.Ordinal), commercial.Describe());
    Assert(commercial.Describe().Contains("sbom=True", StringComparison.Ordinal), commercial.Describe());
}

static void SigtranSecurityPolicyReportsResponseTargets()
{
    SigtranSecurityResponsePolicy policy = SigtranSecurityPolicy.CreateCurrentPolicy();

    Assert(policy.UsesPrivateDisclosure, "security policy should use private disclosure");
    AssertEqual(TimeSpan.FromDays(2), policy.GetResponseTime(SigtranSecuritySeverity.Critical), "critical security response time");
    AssertEqual(TimeSpan.FromDays(7), policy.GetResponseTime(SigtranSecuritySeverity.High), "high security response time");
    AssertEqual(TimeSpan.FromDays(14), policy.GetResponseTime(SigtranSecuritySeverity.Low), "low security response time");
}

static void SigtranCompatibilityPolicyReportsSemVerRules()
{
    SigtranCompatibilityPolicy policy = SigtranCompatibility.CreateCurrentPolicy();

    AssertEqual("net10.0", policy.TargetFramework, "compatibility target framework");
    Assert(policy.UsesSemanticVersioning, "compatibility policy should use SemVer");
    Assert(policy.AllowsBreakingChangesBeforeStable, "pre-stable compatibility should allow breaking changes");
    Assert(policy.StableApiRequiresMajorVersion, "stable breaking changes should require major version");
    Assert(policy.Describe().Contains("semver=True", StringComparison.Ordinal), policy.Describe());
}

static void SigtranObservabilityProfileExposesCommercialSignals()
{
    SigtranObservabilityProfile profile = SigtranObservability.CreateDefaultProfile();

    AssertEqual(4, profile.MetricNames.Count, "observability metric count");
    Assert(profile.MetricNames.Contains("sigtran.m3ua.messages.sent"), "observability should include sent message metric");
    Assert(profile.TraceCategories.Contains("sigtran.trace.packet"), "observability should include packet trace category");
    Assert(profile.HealthSignals.Contains("asp-active"), "observability should include ASP health signal");
    AssertEqual("metrics=4 traces=4 health=4", profile.Describe(), "observability summary");
}

static void SigtranDeploymentProfilesExposeCommercialAndDevelopmentGates()
{
    SigtranDeploymentProfile commercial = SigtranDeploymentProfiles.CreateCommercialLinuxProfile();
    SigtranDeploymentProfile local = SigtranDeploymentProfiles.CreateLocalDevelopmentProfile();

    AssertEqual("commercial-linux", commercial.Name, "commercial deployment profile name");
    AssertEqual(SigtranOperatingSystemFamily.Linux, commercial.OperatingSystem, "commercial deployment OS");
    Assert(commercial.RequiresNativeSctp, "commercial deployment should require native SCTP");
    Assert(commercial.RequiresExternalEvidence, "commercial deployment should require external evidence");
    Assert(!local.RequiresNativeSctp, "local development should not require native SCTP");
    Assert(local.Describe().Contains("security=True", StringComparison.Ordinal), local.Describe());
}

static void SigtranCommercializationStatusSummarizesCommercializationFoundation()
{
    IReadOnlyList<string> capabilities = SigtranCommercializationStatus.GetCompletedCapabilities();

    AssertEqual("Commercialization and Release Hardening", SigtranCommercializationStatus.StatusLabel, "commercialization label");
    AssertEqual(10, SigtranCommercializationStatus.CompletedUnitCount, "commercialization completed unit count");
    AssertEqual(10, capabilities.Count, "commercialization capability count");
    Assert(capabilities.Contains("deployment-profiles"), "commercialization status should include deployment profiles");
    Assert(SigtranCommercializationStatus.Describe().Contains("internalReleaseReady=True", StringComparison.Ordinal), SigtranCommercializationStatus.Describe());
    Assert(SigtranCommercializationStatus.Describe().Contains("commercialReady=False", StringComparison.Ordinal), SigtranCommercializationStatus.Describe());
}

static void SigtranInteropLabScenarioCatalogExposesRequiredScenarios()
{
    IReadOnlyList<SigtranInteropLabScenario> scenarios = SigtranInteropLabScenarios.GetScenarios();

    AssertEqual(3, scenarios.Count, "interop lab scenario count");
    AssertEqual("linux-native-sctp-loopback", scenarios[0].Id, "interop lab first scenario");
    Assert(SigtranInteropLabScenarios.TryGet("EXTERNAL-PEER-M3UA-ASP-TO-SG", out SigtranInteropLabScenario? externalPeer), "external peer lab scenario should exist");
    AssertEqual(SigtranInteropLabScenarioKind.M3uaAspToSignallingGateway, externalPeer!.Kind, "external peer lab scenario kind");
    Assert(externalPeer.RequiredArtifacts.Contains("pcap"), "external peer lab scenario should require PCAP");
    Assert(externalPeer.Describe().Contains("external-sigtran-peer", StringComparison.Ordinal), externalPeer.Describe());
}

static void SigtranInteropLabArtifactManifestValidatesRequiredFiles()
{
    Assert(SigtranInteropLabScenarios.TryGet("external-peer-m3ua-asp-to-sg", out SigtranInteropLabScenario? scenario), "external peer scenario should exist");
    SigtranInteropLabArtifactManifest manifest = new(scenario!.Id);

    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PacketCapture, "artifacts/external-peer/pcap/m3ua-asp.pcapng", "ABC123"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.SdkTrace, "artifacts/external-peer/sdk-trace/m3ua-asp.log"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerConfiguration, "artifacts/external-peer/peer-config/sg.conf"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerLog, "artifacts/external-peer/peer-log/sg.log"));

    Assert(manifest.Satisfies(scenario), "artifact manifest should satisfy external peer scenario");
    AssertEqual(4, manifest.Snapshot().Count, "artifact manifest count");
    AssertEqual("ABC123", manifest.Snapshot()[0].Sha256, "artifact digest");
    Assert(!new SigtranInteropLabArtifactManifest("wrong").Satisfies(scenario), "wrong scenario manifest should not satisfy");
}

static void SigtranInteropLabRunReportIdentifiesPassingEvidence()
{
    Assert(SigtranInteropLabScenarios.TryGet("external-peer-m3ua-asp-to-sg", out SigtranInteropLabScenario? scenario), "external peer scenario should exist");
    SigtranInteropLabArtifactManifest manifest = new(scenario!.Id);
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PacketCapture, "artifacts/external-peer/pcap/m3ua-asp.pcapng"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.SdkTrace, "artifacts/external-peer/sdk-trace/m3ua-asp.log"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerConfiguration, "artifacts/external-peer/peer-config/sg.conf"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerLog, "artifacts/external-peer/peer-log/sg.log"));

    DateTimeOffset startedAt = new(2026, 6, 19, 8, 0, 0, TimeSpan.Zero);
    SigtranInteropLabRunReport report = new(
        scenario,
        manifest,
        SigtranInteropLabRunStatus.Passed,
        startedAt,
        startedAt.AddMinutes(5),
        "ASP lifecycle completed.");

    Assert(report.HasPassingEvidence, report.Describe());
    Assert(report.Describe().Contains("passingEvidence=True", StringComparison.Ordinal), report.Describe());

    SigtranInteropLabRunReport failed = new(scenario, manifest, SigtranInteropLabRunStatus.Failed, startedAt);
    Assert(!failed.HasPassingEvidence, failed.Describe());
}

static void SigtranExternalPeerInteropProfileExposesM3uaAspToSgTemplate()
{
    SigtranInteropPeerProfile profile = SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway();
    AssertEqual("external-sigtran-sg", profile.Id, "external peer id");
    AssertEqual(SigtranInteropPeerRole.SignallingGateway, profile.Role, "external peer role");
    AssertEqual(SigtranInteropPeerProfiles.MaintainedPeerReferenceUrl, profile.ReferenceUrl, "external peer reference URL");

    SigtranInteropLabTemplate template = SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate();
    AssertEqual("external-peer-m3ua-asp-to-sg", template.Scenario.Id, "external peer lab scenario");
    AssertEqual("external-sigtran-sg", template.PeerProfile.Id, "external peer template peer");
    Assert(template.ExpectedMessages.Contains("ASPUP"), "external peer template should include ASPUP");
    Assert(template.ExpectedMessages.Contains("DATA"), "external peer template should include DATA");
    Assert(template.Describe().Contains("messages=11", StringComparison.Ordinal), template.Describe());
}

static void SigtranExternalPeerProfileMarksMaintainedCommercialCandidates()
{
    SigtranInteropPeerProfile profile = SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway();

    AssertEqual(SigtranInteropPeerSupportModel.MaintainedPeerStack, profile.SupportModel, "external peer support model");
    Assert(profile.IsMaintainedCommercialCandidate, profile.Describe());
    Assert(profile.Describe().Contains("support=MaintainedPeerStack", StringComparison.Ordinal), profile.Describe());

    SigtranInteropPeerProfile simulator = new(
        "external-peer-simulator",
        SigtranInteropPeerRole.SignallingGateway,
        "Protocol simulator",
        "https://example.invalid/simulator",
        "SCTP/M3UA",
        "Simulator evidence is useful for development but not commercial promotion.",
        SigtranInteropPeerSupportModel.Simulator);

    Assert(!simulator.IsMaintainedCommercialCandidate, simulator.Describe());
}

static void SigtranMaintainedPeerSelectionPolicyRequiresPackageNeutralEvidenceCriteria()
{
    SigtranMaintainedPeerSelectionPolicy policy = SigtranMaintainedPeerSelectionPolicy.CreateDefault();
    SigtranInteropPeerProfile profile = SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway();
    IReadOnlyList<string> allCriteria = policy.GetCriteria().Select(static criterion => criterion.Id).ToArray();

    AssertEqual(6, policy.GetCriteria().Count, "maintained peer criterion count");
    Assert(policy.GetCriteria().Any(static criterion => criterion.Kind == SigtranMaintainedPeerSelectionCriterionKind.ModernLinuxSupport), "selection policy should require modern Linux support");
    Assert(policy.GetCriteria().Any(static criterion => criterion.Kind == SigtranMaintainedPeerSelectionCriterionKind.LicenseIsolation), "selection policy should require license isolation");

    SigtranMaintainedPeerSelectionReport selected = policy.Evaluate(profile, allCriteria);
    Assert(selected.Selected, selected.Describe());

    SigtranMaintainedPeerSelectionReport missing = policy.Evaluate(profile, ["maintained-upstream", "modern-linux", "native-sctp"]);
    Assert(!missing.Selected, missing.Describe());
    Assert(missing.MissingCriteria.Contains("retained-artifacts"), "selection policy should require retained artifacts");
    Assert(missing.Describe().Contains("missing=3", StringComparison.Ordinal), missing.Describe());
}

static void SigtranMaintainedPeerLabBindingCatalogExposesPackageNeutralDefaults()
{
    SigtranMaintainedPeerLabBinding binding = SigtranMaintainedPeerLabBindings.CreateDefault();
    IReadOnlyList<SigtranMaintainedPeerLabBinding> catalog = SigtranMaintainedPeerLabBindings.CreateCatalog();
    SigtranMaintainedPeerSelectionReport report = binding.EvaluateSelection(SigtranMaintainedPeerSelectionPolicy.CreateDefault());

    AssertEqual("maintained-external-peer-lab", binding.Id, "maintained peer lab binding id");
    AssertEqual("external-sigtran-sg", binding.PeerProfile.Id, "maintained peer lab binding profile");
    AssertEqual("external-peer-package", binding.PackageId, "maintained peer lab binding package");
    AssertEqual("configured-by-lab", binding.PackageVersion, "maintained peer lab binding package version");
    AssertEqual("artifacts/external-peer/maintained", binding.ArtifactRoot, "maintained peer lab binding artifact root");
    AssertEqual(1, catalog.Count, "maintained peer lab binding catalog count");

    Assert(binding.EnvironmentVariables.ContainsKey(SigtranMaintainedPeerLabBindings.PeerIdEnvironmentVariable), "binding should expose peer id environment variable");
    Assert(binding.EnvironmentVariables.ContainsKey(SigtranMaintainedPeerLabBindings.PackageEnvironmentVariable), "binding should expose package environment variable");
    Assert(binding.EnvironmentVariables.ContainsKey(SigtranMaintainedPeerLabBindings.PackageVersionEnvironmentVariable), "binding should expose package version environment variable");
    Assert(binding.EnvironmentVariables.ContainsKey(SigtranMaintainedPeerLabBindings.ArtifactRootEnvironmentVariable), "binding should expose artifact root environment variable");
    Assert(binding.SatisfiedCriterionIds.Contains("license-isolated"), "binding should satisfy license isolation");
    Assert(report.Selected, report.Describe());

    string description = binding.Describe();
    Assert(!description.Contains("OpenSS7", StringComparison.OrdinalIgnoreCase), description);
    Assert(!description.Contains("Osmocom", StringComparison.OrdinalIgnoreCase), description);
    Assert(description.Contains("packageConfigured=True", StringComparison.Ordinal), description);
}

static void SigtranMaintainedPeerLabPrerequisitesReportHostReadiness()
{
    IReadOnlyList<SigtranMaintainedPeerLabPrerequisite> prerequisites = SigtranMaintainedPeerLabPrerequisites.GetDefault();
    IReadOnlyList<string> allPrerequisites = prerequisites.Select(static prerequisite => prerequisite.Id).ToArray();

    AssertEqual(6, prerequisites.Count, "maintained peer lab prerequisite count");
    Assert(prerequisites.Any(static prerequisite => prerequisite.Kind == SigtranMaintainedPeerLabPrerequisiteKind.NativeSctp), "prerequisites should include native SCTP");
    Assert(prerequisites.Any(static prerequisite => prerequisite.Kind == SigtranMaintainedPeerLabPrerequisiteKind.PacketCapture), "prerequisites should include packet capture");

    SigtranMaintainedPeerLabPrerequisiteReport ready = SigtranMaintainedPeerLabPrerequisites.Evaluate(allPrerequisites);
    Assert(ready.Ready, ready.Describe());
    AssertEqual(0, ready.MissingPrerequisiteIds.Count, "ready maintained peer prerequisite missing count");

    SigtranMaintainedPeerLabPrerequisiteReport missing = SigtranMaintainedPeerLabPrerequisites.Evaluate(["linux-host", "dotnet-10-runtime"]);
    Assert(!missing.Ready, missing.Describe());
    Assert(missing.MissingPrerequisiteIds.Contains("native-sctp-tools"), "missing prerequisite report should include native SCTP tools");
    Assert(missing.MissingPrerequisiteIds.Contains("external-peer-package"), "missing prerequisite report should include external peer package");
    Assert(missing.Describe().Contains("missing=4", StringComparison.Ordinal), missing.Describe());
}

static void SigtranMaintainedPeerLabConfigurationValidatesEnvironmentContracts()
{
    SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
    SigtranMaintainedPeerLabConfigurationValidation validation = configuration.Validate();

    Assert(validation.IsValid, validation.Describe());
    AssertEqual("M3UA", configuration.Adaptation, "maintained peer lab adaptation");
    AssertEqual((uint)100, configuration.RoutingContext, "maintained peer lab routing context");
    Assert(configuration.Describe().Contains("traffic=loadshare", StringComparison.Ordinal), configuration.Describe());

    Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PEER_NAME"] = "phase27-maintained-peer",
        ["LOCAL_IP"] = "127.0.0.1",
        ["LOCAL_SCTP_PORT"] = "2905",
        ["REMOTE_IP"] = "127.0.0.1",
        ["REMOTE_SCTP_PORT"] = "2906",
        ["SIGTRAN_ADAPTATION"] = "M3UA",
        ["NETWORK_INDICATOR"] = "2",
        ["SERVICE_INDICATOR"] = "3",
        ["OPC"] = "1",
        ["DPC"] = "2",
        ["ROUTING_CONTEXT"] = "100",
        ["TRAFFIC_MODE"] = "loadshare",
        ["ARTIFACT_ROOT"] = "artifacts/external-peer/phase27"
    };

    SigtranMaintainedPeerLabConfiguration parsed = SigtranMaintainedPeerLabConfigurations.FromEnvironment(environment);
    AssertEqual("phase27-maintained-peer", parsed.PeerName, "parsed maintained peer name");
    AssertEqual(2906, parsed.RemoteSctpPort, "parsed maintained peer remote SCTP port");
    AssertEqual("artifacts/external-peer/phase27", parsed.ArtifactRoot, "parsed maintained peer artifact root");
    Assert(parsed.Validate().IsValid, parsed.Validate().Describe());

    SigtranMaintainedPeerLabConfiguration invalid = new(
        "bad-peer",
        "not-an-ip",
        0,
        "127.0.0.1",
        70000,
        "SUA",
        2,
        3,
        1,
        2,
        0,
        "invalid",
        "artifacts/external-peer/bad");

    SigtranMaintainedPeerLabConfigurationValidation invalidReport = invalid.Validate();
    Assert(!invalidReport.IsValid, invalidReport.Describe());
    Assert(invalidReport.Errors.Contains("local-ip-invalid"), "invalid config should report local IP");
    Assert(invalidReport.Errors.Contains("local-sctp-port-invalid"), "invalid config should report local SCTP port");
    Assert(invalidReport.Errors.Contains("remote-sctp-port-invalid"), "invalid config should report remote SCTP port");
    Assert(invalidReport.Errors.Contains("adaptation-unsupported"), "invalid config should report unsupported adaptation");
    Assert(invalidReport.Errors.Contains("routing-context-required"), "invalid config should report missing routing context");
    Assert(invalidReport.Errors.Contains("traffic-mode-unsupported"), "invalid config should report unsupported traffic mode");
}

static void SigtranMaintainedPeerLabArtifactPlanCoversRetainedEvidencePaths()
{
    SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
    SigtranMaintainedPeerLabArtifactPlan plan = SigtranMaintainedPeerLabArtifactPlans.CreateDefault(configuration, "phase27-unit5");

    AssertEqual("phase27-unit5", plan.RunId, "maintained peer artifact plan run id");
    AssertEqual("artifacts/external-peer/maintained", plan.ArtifactRoot, "maintained peer artifact plan root");
    AssertEqual(6, plan.Items.Count, "maintained peer artifact plan item count");
    Assert(plan.CoversRequiredArtifacts, plan.Describe());
    Assert(plan.Items.Any(static item => item.Kind == SigtranMaintainedPeerLabArtifactKind.PacketCapture && item.Path.EndsWith("/pcap/phase27-unit5.pcap", StringComparison.Ordinal)), "artifact plan should include PCAP path");
    Assert(plan.Items.Any(static item => item.Kind == SigtranMaintainedPeerLabArtifactKind.PeerConfiguration && item.Path.Contains("/config/", StringComparison.Ordinal)), "artifact plan should include config path");
    Assert(plan.Items.Any(static item => item.Kind == SigtranMaintainedPeerLabArtifactKind.ComparisonReport && item.Path.Contains("/comparison/", StringComparison.Ordinal)), "artifact plan should include comparison path");
    Assert(plan.Describe().Contains("requiredReady=True", StringComparison.Ordinal), plan.Describe());
}

static void SigtranMaintainedPeerLabCommandPlanCoversExecutionSteps()
{
    SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
    SigtranMaintainedPeerLabArtifactPlan artifactPlan = SigtranMaintainedPeerLabArtifactPlans.CreateDefault(configuration, "phase27-unit6");
    SigtranMaintainedPeerLabCommandPlan commandPlan = SigtranMaintainedPeerLabCommandPlans.CreateDefault(configuration, artifactPlan);

    AssertEqual(6, commandPlan.Commands.Count, "maintained peer lab command count");
    Assert(commandPlan.CoversRequiredCommandKinds, commandPlan.Describe());
    AssertEqual(SigtranMaintainedPeerLabCommandKind.Prepare, commandPlan.Commands[0].Kind, "first maintained peer lab command");
    AssertEqual(SigtranMaintainedPeerLabCommandKind.Collect, commandPlan.Commands[^1].Kind, "last maintained peer lab command");
    Assert(commandPlan.Commands.Any(static command => command.CommandLine.Contains("tcpdump", StringComparison.Ordinal)), "command plan should include packet capture");
    Assert(commandPlan.Commands.Any(static command => command.ExpectedArtifactKinds.Contains(SigtranMaintainedPeerLabArtifactKind.SdkTrace)), "command plan should produce SDK trace");
    Assert(commandPlan.Describe().Contains("ready=True", StringComparison.Ordinal), commandPlan.Describe());
}

static void SigtranMaintainedPeerLabTrafficVectorsExposeComparableSequence()
{
    IReadOnlyList<SigtranMaintainedPeerLabTrafficVector> vectors = SigtranMaintainedPeerLabTrafficVectors.GetDefault();
    IReadOnlyList<string> sequence = SigtranMaintainedPeerLabTrafficVectors.GetExpectedMessageSequence();

    AssertEqual(3, vectors.Count, "maintained peer lab traffic vector count");
    Assert(vectors.All(static vector => vector.IsComparable), "traffic vectors should be comparable");
    Assert(vectors.Any(static vector => vector.Kind == SigtranMaintainedPeerLabTrafficVectorKind.PayloadData && vector.RequiresPayload), "traffic vectors should include payload DATA");
    Assert(sequence.Contains("ASPUP"), "traffic sequence should include ASPUP");
    Assert(sequence.Contains("BEAT_ACK"), "traffic sequence should include heartbeat acknowledgement");
    Assert(sequence.Contains("DATA"), "traffic sequence should include DATA");

    SigtranTraceComparisonReport comparison = SigtranTraceComparison.Compare(sequence, sequence);
    Assert(comparison.Passed, comparison.Describe());
}

static void SigtranMaintainedPeerLabEvidenceGateRequiresCompleteRetainedArtifacts()
{
    SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
    SigtranMaintainedPeerLabArtifactPlan artifactPlan = SigtranMaintainedPeerLabArtifactPlans.CreateDefault(configuration, "phase27-unit8");
    IReadOnlyList<string> allPrerequisites = SigtranMaintainedPeerLabPrerequisites.GetDefault()
        .Select(static prerequisite => prerequisite.Id)
        .ToArray();

    SigtranMaintainedPeerLabEvidenceReport ready = new(
        artifactPlan,
        SigtranMaintainedPeerLabPrerequisites.Evaluate(allPrerequisites),
        configuration.Validate(),
        SigtranMaintainedPeerLabEvidence.CreateDigestCoveredArtifacts(artifactPlan, "0123456789abcdef"),
        comparisonPassed: true);

    Assert(ready.PromotionReady, ready.Describe());
    Assert(ready.HasRequiredArtifacts, ready.Describe());
    Assert(ready.HasDigestCoverage, ready.Describe());

    SigtranMaintainedPeerLabEvidenceReport missingDigest = new(
        artifactPlan,
        SigtranMaintainedPeerLabPrerequisites.Evaluate(allPrerequisites),
        configuration.Validate(),
        [new(SigtranMaintainedPeerLabArtifactKind.PacketCapture, "artifacts/external-peer/maintained/pcap/missing.pcap", retained: true)],
        comparisonPassed: true);

    Assert(!missingDigest.PromotionReady, missingDigest.Describe());
    Assert(!missingDigest.HasRequiredArtifacts, missingDigest.Describe());
    Assert(!missingDigest.HasDigestCoverage, missingDigest.Describe());
    Assert(missingDigest.Describe().Contains("promotion=False", StringComparison.Ordinal), missingDigest.Describe());
}

static void SigtranMaintainedPeerLabCiProfileIsManualAndSelfHosted()
{
    SigtranMaintainedPeerLabCiProfile profile = SigtranMaintainedPeerLabCi.CreateDefault();

    AssertEqual("maintained-peer-lab", profile.Name, "maintained peer lab CI profile name");
    Assert(profile.ManualDispatchOnly, profile.Describe());
    Assert(profile.RequiresSelfHostedLinux, profile.Describe());
    Assert(!profile.SafeForDefaultCi, profile.Describe());
    Assert(profile.RequiredEnvironmentVariables.Contains(SigtranMaintainedPeerLabBindings.PackageEnvironmentVariable), "CI profile should require peer package variable");
    Assert(profile.RequiredEnvironmentVariables.Contains("LOCAL_SCTP_PORT"), "CI profile should require local SCTP port");
    Assert(profile.ArtifactPatterns.Any(static pattern => pattern.Contains("/pcap/", StringComparison.Ordinal)), "CI profile should upload PCAP artifacts");
    Assert(profile.ArtifactPatterns.Any(static pattern => pattern.Contains("/comparison/", StringComparison.Ordinal)), "CI profile should upload comparison artifacts");
}

static void SigtranMaintainedPeerLabStatusSeparatesFoundationFromEvidence()
{
    IReadOnlyList<string> capabilities = SigtranMaintainedPeerLabStatus.GetCompletedCapabilities();
    SigtranMaintainedPeerLabStatusReport foundation = SigtranMaintainedPeerLabStatus.GetFoundationReport();

    AssertEqual(10, SigtranMaintainedPeerLabStatus.CompletedUnitCount, "maintained peer lab completed unit count");
    AssertEqual(10, capabilities.Count, "maintained peer lab completed capability count");
    Assert(capabilities.Contains("package-neutral-binding-catalog"), "status should include binding catalog capability");
    Assert(foundation.FoundationReady, foundation.Describe());
    Assert(!foundation.CommercialReady, foundation.Describe());
    Assert(foundation.Blockers.Contains("real-maintained-peer-run-required"), "foundation status should require real lab run");

    SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
    SigtranMaintainedPeerLabArtifactPlan artifactPlan = SigtranMaintainedPeerLabArtifactPlans.CreateDefault(configuration, "phase27-unit10");
    IReadOnlyList<string> allPrerequisites = SigtranMaintainedPeerLabPrerequisites.GetDefault()
        .Select(static prerequisite => prerequisite.Id)
        .ToArray();
    SigtranMaintainedPeerLabEvidenceReport evidence = new(
        artifactPlan,
        SigtranMaintainedPeerLabPrerequisites.Evaluate(allPrerequisites),
        configuration.Validate(),
        SigtranMaintainedPeerLabEvidence.CreateDigestCoveredArtifacts(artifactPlan, "0123456789abcdef"),
        comparisonPassed: true);

    SigtranMaintainedPeerLabStatusReport commercial = SigtranMaintainedPeerLabStatus.FromEvidence(evidence);
    Assert(commercial.CommercialReady, commercial.Describe());
    AssertEqual(0, commercial.Blockers.Count, "commercial maintained peer lab blocker count");
}

static void SigtranMaintainedPeerLabRunManifestAggregatesExecutableContracts()
{
    SigtranMaintainedPeerLabRunManifest manifest = SigtranMaintainedPeerLabRunManifests.CreateDefault("phase28-unit1");

    AssertEqual("phase28-unit1", manifest.RunId, "maintained peer lab run manifest id");
    AssertEqual("maintained-external-peer-lab", manifest.Binding.Id, "maintained peer lab run manifest binding");
    AssertEqual("phase28-unit1", manifest.ArtifactPlan.RunId, "maintained peer lab run manifest artifact run id");
    AssertEqual(6, manifest.CommandPlan.Commands.Count, "maintained peer lab run manifest command count");
    AssertEqual(3, manifest.TrafficVectors.Count, "maintained peer lab run manifest vector count");
    Assert(manifest.IsExecutableContract, manifest.Describe());
    Assert(manifest.Describe().Contains("executable=True", StringComparison.Ordinal), manifest.Describe());
}

static void SigtranTraceComparisonReportsOrderedMismatches()
{
    SigtranInteropLabTemplate template = SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate();
    SigtranTraceComparisonReport passed = SigtranTraceComparison.Compare(template.ExpectedMessages, template.ExpectedMessages);

    Assert(passed.Passed, passed.Describe());
    AssertEqual(0, passed.Mismatches.Count, "passing comparison mismatch count");

    SigtranTraceComparisonReport failed = SigtranTraceComparison.Compare(
        ["ASPUP", "ASPUP_ACK", "ASPAC", "ASPAC_ACK"],
        ["ASPUP", "ASPUP_ACK", "ASPAC", "DATA"]);

    Assert(!failed.Passed, failed.Describe());
    AssertEqual(1, failed.Mismatches.Count, "failed comparison mismatch count");
    AssertEqual(3, failed.Mismatches[0].Index, "failed comparison mismatch index");
    AssertEqual("ASPAC_ACK", failed.Mismatches[0].Expected, "failed comparison expected message");
    AssertEqual("DATA", failed.Mismatches[0].Actual, "failed comparison actual message");
}

static void SigtranInteropEvidencePromotionRequiresPassingLabRun()
{
    SigtranInteropLabTemplate template = SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate();
    SigtranInteropLabArtifactManifest manifest = new(template.Scenario.Id);
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PacketCapture, "artifacts/external-peer/pcap/m3ua-asp.pcapng"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.SdkTrace, "artifacts/external-peer/sdk-trace/m3ua-asp.log"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerConfiguration, "artifacts/external-peer/peer-config/sg.conf"));
    manifest.Add(new SigtranInteropLabArtifact(SigtranInteropLabArtifactKind.PeerLog, "artifacts/external-peer/peer-log/sg.log"));

    DateTimeOffset startedAt = new(2026, 6, 19, 9, 0, 0, TimeSpan.Zero);
    SigtranInteropLabRunReport passed = new(template.Scenario, manifest, SigtranInteropLabRunStatus.Passed, startedAt, startedAt.AddMinutes(4));
    SigtranInteropEvidenceItem evidence = SigtranInteropEvidencePromotion.Promote(passed, "lab-external-peer-pass");

    AssertEqual("lab-external-peer-pass", evidence.Id, "promoted evidence id");
    AssertEqual("external-sigtran-peer", evidence.PeerStack, "promoted evidence peer");
    AssertEqual(SigtranInteropEvidenceResult.Passed, evidence.Result, "promoted evidence result");
    Assert(evidence.TraceReference.Contains("pcap", StringComparison.Ordinal), evidence.TraceReference);

    SigtranInteropLabRunReport failed = new(template.Scenario, manifest, SigtranInteropLabRunStatus.Failed, startedAt);
    AssertThrows<InvalidOperationException>(() => SigtranInteropEvidencePromotion.Promote(failed));
}

static void SigtranInteropLabCiProfileIsOptIn()
{
    SigtranInteropLabCiProfile profile = SigtranInteropLabCiProfiles.CreateDefault();
    AssertEqual("SIGTRAN_INTEROP_LAB", profile.EnableVariable, "interop lab enable variable");
    AssertEqual("SIGTRAN_INTEROP_LAB_ARTIFACT_ROOT", profile.ArtifactRootVariable, "interop lab artifact root variable");
    Assert(profile.RequiredVariables.Contains("SIGTRAN_INTEROP_PEER"), "interop lab peer variable should be required");
    Assert(profile.Commands.Any(static command => command.Contains("dotnet build", StringComparison.Ordinal)), "interop lab profile should include build command");

    Assert(!profile.IsEnabled(new Dictionary<string, string>()), "interop lab profile should be disabled by default");
    Assert(profile.IsEnabled(new Dictionary<string, string> { ["SIGTRAN_INTEROP_LAB"] = "true" }), "interop lab profile should accept true");
    Assert(profile.IsEnabled(new Dictionary<string, string> { ["SIGTRAN_INTEROP_LAB"] = "1" }), "interop lab profile should accept 1");
}

static void SigtranInteropLabReadinessReportsFoundationAndEvidenceGates()
{
    SigtranInteropLabReadinessReport report = SigtranInteropLabReadiness.GetReport();

    Assert(report.FoundationReady, report.Describe());
    Assert(!report.HasPassingExternalEvidence, "external lab evidence should stay false until real artifacts are added");
    Assert(!report.ProductionReady, report.Describe());
    Assert(report.Describe().Contains("foundationReady=True", StringComparison.Ordinal), report.Describe());
}

static void SigtranCommercialReadinessUsesInteropLabProductionGate()
{
    SigtranInteropLabReadinessReport labReadiness = SigtranInteropLabReadiness.GetReport();
    SigtranCommercialReadinessReport commercial = SigtranCommercialReadiness.GetReport();

    AssertEqual(labReadiness.ProductionReady, commercial.HasExternalInteroperabilityEvidence, "commercial external interop gate");
    Assert(!commercial.HasExternalInteroperabilityEvidence, "commercial readiness should wait for real lab evidence");
    Assert(!commercial.CommercialReady, commercial.Describe());
}

static void SigtranInteropLabStatusSummarizesInteropLabFoundation()
{
    IReadOnlyList<string> capabilities = SigtranInteropLabStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranInteropLabStatus.CompletedUnitCount, "interoperability lab completed unit count");
    AssertEqual(10, capabilities.Count, "interoperability lab capability count");
    Assert(capabilities.Contains("commercial-readiness-gate-integration"), "interoperability lab status should include commercial readiness integration");
    Assert(SigtranInteropLabStatus.Describe().Contains("foundationReady=True", StringComparison.Ordinal), SigtranInteropLabStatus.Describe());
    Assert(SigtranInteropLabStatus.Describe().Contains("productionReady=False", StringComparison.Ordinal), SigtranInteropLabStatus.Describe());
}

static void SigtranReleaseAutomationPlanExposesDeterministicReleaseSteps()
{
    SigtranReleaseAutomationPlan plan = SigtranReleaseAutomation.CreateDefaultPlan();

    AssertEqual("release-default", plan.Id, "release automation plan id");
    AssertEqual("10.0.x", plan.DotNetVersion, "release automation .NET version");
    AssertEqual(6, plan.Steps.Count, "release automation step count");
    AssertEqual(SigtranReleaseAutomationStepKind.Restore, plan.Steps[0].Kind, "release automation first step");
    AssertEqual(SigtranReleaseAutomationStepKind.Publish, plan.Steps[^1].Kind, "release automation final step");
    Assert(plan.GetCommands().Any(static command => command.Contains("dotnet pack", StringComparison.Ordinal)), "release automation should include pack command");
    Assert(plan.Describe().Contains("steps=6", StringComparison.Ordinal), plan.Describe());
}

static void SigtranReleaseArtifactManifestTracksPackageArtifactsAndDigests()
{
    SigtranReleaseArtifactManifest manifest = new("Sigtran.NET", "1.0.0-alpha.1");
    manifest.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.NuGetPackage, "artifacts/Sigtran.NET.1.0.0-alpha.1.nupkg", "abc"));
    manifest.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.SymbolPackage, "artifacts/Sigtran.NET.1.0.0-alpha.1.snupkg", "def"));
    manifest.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.ReleaseNotes, "artifacts/release-notes.md", "ghi"));

    AssertEqual(3, manifest.Snapshot().Count, "release artifact count");
    Assert(manifest.HasRequiredPackageArtifacts(), "release manifest should include package artifacts");
    Assert(manifest.AllArtifactsHaveDigests(), "release artifacts should have digests");

    SigtranReleaseArtifactManifest incomplete = new("Sigtran.NET", "1.0.0-alpha.1");
    incomplete.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.NuGetPackage, "artifacts/Sigtran.NET.1.0.0-alpha.1.nupkg"));
    Assert(!incomplete.HasRequiredPackageArtifacts(), "release manifest should require symbol package");
    Assert(!incomplete.AllArtifactsHaveDigests(), "release manifest should require digests");
}

static void SigtranSbomPlanMarksCommercialReleaseRequirement()
{
    SigtranSbomPlan plan = SigtranSbom.CreateDefaultPlan();

    AssertEqual(SigtranSbomFormat.SpdxJson, plan.Format, "SBOM format");
    AssertEqual("Microsoft.Sbom.Tool", plan.ToolName, "SBOM tool");
    Assert(plan.IsRequiredForCommercialRelease, "SBOM should be required for commercial release");
    Assert(plan.OutputPath.EndsWith(".spdx.json", StringComparison.Ordinal), plan.OutputPath);
    Assert(plan.Describe().Contains("required=True", StringComparison.Ordinal), plan.Describe());
}

static void SigtranPackageSigningPlanMarksCommercialReleaseRequirement()
{
    SigtranPackageSigningPlan plan = SigtranPackageSigning.CreateDefaultPlan();

    AssertEqual(SigtranPackageSigningMode.Author, plan.Mode, "package signing mode");
    Assert(plan.IsRequiredForCommercialRelease, "package signing should be required for commercial release");
    Assert(plan.HasSigningMaterialReferences, plan.Describe());
    Assert(plan.TimestampAuthorityUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase), plan.TimestampAuthorityUrl);
    Assert(plan.Describe().Contains("required=True", StringComparison.Ordinal), plan.Describe());
}

static void SigtranReleaseProvenanceRecordsSourceCommitAndArtifactManifest()
{
    SigtranReleaseProvenance provenance = SigtranReleaseProvenanceFactory.Create("abcdef123456", "artifacts/release-manifest.json");

    AssertEqual("https://github.com/araditc/Sigtran.NET", provenance.SourceRepository, "release provenance repository");
    AssertEqual("abcdef123456", provenance.CommitSha, "release provenance commit");
    AssertEqual("release-default", provenance.WorkflowName, "release provenance workflow");
    Assert(provenance.HasRequiredReferences, provenance.Describe());
    Assert(provenance.Describe().Contains("manifest=artifacts/release-manifest.json", StringComparison.Ordinal), provenance.Describe());
}

static void SigtranReleaseNotesRequireSemVerAndChangeEntries()
{
    SigtranReleaseNotes notes = SigtranReleaseNotesFactory.CreateAlpha("1.0.0-alpha.1");

    AssertEqual("1.0.0-alpha.1", notes.Version, "release notes version");
    Assert(notes.IsPublishable, notes.Describe());
    Assert(notes.Changes.Count > 0, "release notes should include changes");
    AssertEqual(0, notes.BreakingChanges.Count, "alpha release notes breaking changes");

    SigtranReleaseNotes invalid = new("alpha", "Invalid release", [], []);
    Assert(!invalid.IsPublishable, invalid.Describe());
}

static void SigtranPublishChannelsSeparatePrereleaseAndStableRules()
{
    IReadOnlyList<SigtranPublishChannel> channels = SigtranPublishChannels.GetChannels();

    AssertEqual(4, channels.Count, "publish channel count");
    SigtranPublishChannel alpha = channels.Single(static channel => channel.Kind == SigtranPublishChannelKind.Alpha);
    SigtranPublishChannel stable = channels.Single(static channel => channel.Kind == SigtranPublishChannelKind.Stable);

    Assert(alpha.AcceptsVersion("1.0.0-alpha.1"), "alpha channel should accept prerelease");
    Assert(!alpha.RequiresCommercialReadiness, "alpha channel should not require commercial readiness");
    Assert(!stable.AcceptsVersion("1.0.0-alpha.1"), "stable channel should reject prerelease");
    Assert(stable.AcceptsVersion("1.0.0"), "stable channel should accept stable SemVer");
    Assert(stable.RequiresCommercialReadiness, "stable channel should require commercial readiness");
}

static void SigtranReleaseGateEvaluatesArtifactNotesProvenanceAndChannelReadiness()
{
    SigtranReleaseArtifactManifest manifest = new("Sigtran.NET", "1.0.0-alpha.1");
    manifest.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.NuGetPackage, "artifacts/Sigtran.NET.1.0.0-alpha.1.nupkg", "abc"));
    manifest.Add(new SigtranReleaseArtifact(SigtranReleaseArtifactKind.SymbolPackage, "artifacts/Sigtran.NET.1.0.0-alpha.1.snupkg", "def"));
    SigtranReleaseNotes notes = SigtranReleaseNotesFactory.CreateAlpha("1.0.0-alpha.1");
    SigtranReleaseProvenance provenance = SigtranReleaseProvenanceFactory.Create("abcdef123456", "artifacts/release-manifest.json");
    SigtranPublishChannel alpha = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Alpha);
    SigtranPublishChannel stable = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Stable);

    SigtranReleaseGateResult alphaResult = SigtranReleaseGate.Evaluate(alpha, manifest, notes, provenance, SigtranCommercialReadiness.GetReport());
    Assert(alphaResult.CanPublish, alphaResult.Describe());

    SigtranReleaseGateResult stableResult = SigtranReleaseGate.Evaluate(stable, manifest, notes, provenance, SigtranCommercialReadiness.GetReport());
    Assert(!stableResult.CanPublish, stableResult.Describe());
    Assert(stableResult.Reasons.Contains("channel-version-rejected"), "stable gate should reject prerelease version");
    Assert(stableResult.Reasons.Contains("commercial-readiness-required"), "stable gate should require commercial readiness");
}

static void SigtranReleaseCiProfileDeclaresTriggersSecretsAndPlan()
{
    SigtranReleaseCiProfile profile = SigtranReleaseCiProfiles.CreateDefault();

    AssertEqual("release", profile.WorkflowName, "release CI workflow name");
    Assert(profile.Triggers.Contains("workflow_dispatch"), "release CI profile should support manual dispatch");
    Assert(profile.Triggers.Contains("tag:v*"), "release CI profile should support version tags");
    Assert(profile.RequiredSecrets.Contains("NUGET_API_KEY"), "release CI profile should require NuGet API key");
    Assert(profile.RequiredSecrets.Contains("SIGNING_CERTIFICATE"), "release CI profile should require signing certificate");
    Assert(profile.IsRunnable, profile.Describe());
}

static void SigtranReleaseAutomationStatusSummarizesReleaseAutomationFoundation()
{
    IReadOnlyList<string> capabilities = SigtranReleaseAutomationStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranReleaseAutomationStatus.CompletedUnitCount, "release automation completed unit count");
    AssertEqual(10, capabilities.Count, "release automation capability count");
    Assert(capabilities.Contains("release-gate-evaluator"), "release automation status should include release gate evaluator");
    Assert(SigtranReleaseAutomationStatus.FoundationReady, SigtranReleaseAutomationStatus.Describe());
    Assert(!SigtranReleaseAutomationStatus.CommercialStableReleaseReady, SigtranReleaseAutomationStatus.Describe());
}

static void SigtranDeveloperExperienceCatalogExposesAdoptionAreas()
{
    IReadOnlyList<SigtranDeveloperExperienceCapability> capabilities = SigtranDeveloperExperience.GetCapabilities();

    AssertEqual(5, capabilities.Count, "developer experience capability count");
    Assert(capabilities.Any(static capability => capability.Area == SigtranDeveloperExperienceArea.Quickstart), "developer experience should include quickstart");
    Assert(capabilities.Any(static capability => capability.Area == SigtranDeveloperExperienceArea.Troubleshooting), "developer experience should include troubleshooting");
    Assert(capabilities.Any(static capability => capability.Id == "adoption-gates"), "developer experience should include adoption gates");
}

static void SigtranM3uaQuickstartExposesOrderedAspToSgSteps()
{
    SigtranQuickstartGuide guide = SigtranQuickstarts.CreateM3uaAspToSg();

    AssertEqual("quickstart-m3ua-asp-to-sg", guide.Id, "M3UA quickstart id");
    AssertEqual(5, guide.Steps.Count, "M3UA quickstart step count");
    AssertEqual("SctpConnectionOptions", guide.Steps[0].Api, "M3UA quickstart first API");
    Assert(guide.Steps.Any(static step => step.Api == "M3uaAspClient"), "M3UA quickstart should include ASP client");
    Assert(guide.Describe().Contains("steps=5", StringComparison.Ordinal), guide.Describe());
}

static void SigtranSampleTemplatesMapSampleIdsToEnvironments()
{
    IReadOnlyList<SigtranSampleTemplate> templates = SigtranSampleTemplates.GetTemplates();

    AssertEqual(3, templates.Count, "sample template count");
    Assert(templates.Any(static template => template.SampleId == "local-tcp-m3ua" && template.Environment == SigtranSampleTemplateEnvironment.LocalDevelopment), "local sample template should exist");
    Assert(templates.Any(static template => template.SampleId == "m3ua-asp-to-sg" && template.Environment == SigtranSampleTemplateEnvironment.InteropLab), "interop lab sample template should exist");
    foreach (SigtranSampleTemplate template in templates)
    {
        Assert(SigtranSampleCatalog.TryGet(template.SampleId, out _), $"sample template '{template.SampleId}' should reference catalog sample");
    }
}

static void SigtranConfigurationProfilesSeparateDevelopmentLabAndProduction()
{
    IReadOnlyList<SigtranConfigurationProfile> profiles = SigtranConfigurationProfiles.GetProfiles();

    AssertEqual(3, profiles.Count, "configuration profile count");
    SigtranConfigurationProfile development = profiles.Single(static profile => profile.Kind == SigtranConfigurationProfileKind.Development);
    SigtranConfigurationProfile production = profiles.Single(static profile => profile.Kind == SigtranConfigurationProfileKind.Production);

    AssertEqual("tcp-adapter", development.Transport, "development profile transport");
    Assert(!development.RequiresExternalEvidence, "development profile should not require external evidence");
    AssertEqual("native-sctp", production.Transport, "production profile transport");
    Assert(production.RequiresExternalEvidence, "production profile should require external evidence");
}

static void SigtranTroubleshootingIndexMapsSymptomsToNextActions()
{
    IReadOnlyList<SigtranTroubleshootingEntry> entries = SigtranTroubleshooting.GetEntries();

    AssertEqual(4, entries.Count, "troubleshooting entry count");
    Assert(entries.Any(static entry => entry.Id == "native-sctp-unavailable" && entry.Category == SigtranTroubleshootingCategory.Transport), "transport troubleshooting entry should exist");
    Assert(entries.Any(static entry => entry.Id == "data-unrouted" && entry.NextAction.Contains("route table", StringComparison.OrdinalIgnoreCase)), "routing troubleshooting entry should exist");
    Assert(entries.Any(static entry => entry.Category == SigtranTroubleshootingCategory.Interoperability), "interop troubleshooting entry should exist");
}

static void SigtranApiReferenceIndexExposesOnboardingApis()
{
    IReadOnlyList<SigtranApiReferenceEntry> entries = SigtranApiReferenceIndex.GetEntries();

    AssertEqual(5, entries.Count, "API reference entry count");
    Assert(entries.Any(static entry => entry.Name == "M3uaAspClient" && entry.Area == "M3UA"), "API reference should include ASP client");
    Assert(entries.Any(static entry => entry.Name == "SctpConnectionOptions" && entry.Area == "SCTP"), "API reference should include SCTP options");
    Assert(entries.Any(static entry => entry.Area == "Diagnostics"), "API reference should include diagnostics");
}

static void SigtranAdoptionGatesSeparateDeveloperReadinessFromEnterpriseProduction()
{
    SigtranAdoptionGateReport report = SigtranAdoptionGates.GetReport();

    Assert(report.DeveloperAdoptionReady, "developer adoption foundation should be ready");
    Assert(!report.CommercialReady, "commercial readiness should still require external gates");
    Assert(!report.EnterpriseProductionReady, "enterprise production adoption should wait for commercial readiness");
}

static void SigtranDocumentationReadinessReportsDeveloperDocsGate()
{
    SigtranDocumentationReadinessReport report = SigtranDocumentationReadiness.GetReport();

    Assert(report.HasRoadmap, "documentation readiness should include roadmap");
    Assert(report.HasQuickstart, "documentation readiness should include quickstart");
    Assert(report.HasApiIndex, "documentation readiness should include API index");
    Assert(report.HasTroubleshooting, "documentation readiness should include troubleshooting");
    Assert(report.DeveloperDocsReady, "developer docs should be ready");
}

static void SigtranDeveloperExperienceCiProfileRequiresDocsAndAdoptionGates()
{
    SigtranDeveloperExperienceCiProfile profile = SigtranDeveloperExperienceCi.CreateDefault();

    AssertEqual("developer-experience", profile.Name, "DX CI profile name");
    Assert(profile.Commands.Any(static command => command.Contains("dotnet build", StringComparison.Ordinal)), "DX CI profile should include build");
    Assert(profile.RequiresDocumentationReadiness, "DX CI profile should require documentation readiness");
    Assert(profile.RequiresAdoptionReadiness, "DX CI profile should require adoption readiness");
}

static void SigtranDeveloperExperienceStatusSummarizesDeveloperExperienceFoundation()
{
    IReadOnlyList<string> capabilities = SigtranDeveloperExperienceStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranDeveloperExperienceStatus.CompletedUnitCount, "developer experience completed unit count");
    AssertEqual(10, capabilities.Count, "developer experience capability count");
    Assert(capabilities.Contains("developer-experience-ci-profile"), "developer experience status should include DX CI profile");
    Assert(SigtranDeveloperExperienceStatus.FoundationReady, SigtranDeveloperExperienceStatus.Describe());
    Assert(!SigtranDeveloperExperienceStatus.EnterpriseProductionReady, SigtranDeveloperExperienceStatus.Describe());
}

static void SigtranOperationsCatalogExposesProductionSupportAreas()
{
    IReadOnlyList<SigtranOperationsCapability> capabilities = SigtranOperations.GetCapabilities();

    AssertEqual(5, capabilities.Count, "operations capability count");
    Assert(capabilities.Any(static capability => capability.Area == SigtranOperationsArea.Runbooks), "operations should include runbooks");
    Assert(capabilities.Any(static capability => capability.Area == SigtranOperationsArea.Health), "operations should include health checks");
    Assert(capabilities.Any(static capability => capability.Id == "support-handbook"), "operations should include support handbook");
}

static void SigtranRunbookCatalogExposesOperationalRecoveryPaths()
{
    IReadOnlyList<SigtranRunbook> runbooks = SigtranRunbooks.GetRunbooks();

    AssertEqual(4, runbooks.Count, "runbook count");
    Assert(runbooks.Any(static runbook => runbook.Kind == SigtranRunbookKind.TransportOutage), "runbooks should include transport outage");
    Assert(runbooks.Any(static runbook => runbook.Kind == SigtranRunbookKind.ReleaseRollback), "runbooks should include release rollback");
    Assert(runbooks.All(static runbook => runbook.FirstAction.Length > 0), "runbooks should define first actions");
}

static void SigtranIncidentResponseTargetsDefineSeverityTiming()
{
    IReadOnlyList<SigtranIncidentResponseTarget> targets = SigtranIncidentResponse.GetTargets();

    AssertEqual(4, targets.Count, "incident response target count");
    SigtranIncidentResponseTarget critical = targets.Single(static target => target.Severity == SigtranIncidentSeverity.Critical);
    SigtranIncidentResponseTarget low = targets.Single(static target => target.Severity == SigtranIncidentSeverity.Low);

    AssertEqual(TimeSpan.FromMinutes(15), critical.AcknowledgeWithin, "critical incident acknowledgement");
    Assert(critical.UpdateWithin < low.UpdateWithin, "critical incidents should update faster than low incidents");
}

static void SigtranHealthCheckMatrixCoversTransportProtocolEvidenceAndRelease()
{
    IReadOnlyList<SigtranHealthCheckDefinition> checks = SigtranHealthChecks.GetDefinitions();

    AssertEqual(5, checks.Count, "health check count");
    Assert(checks.Any(static check => check.Area == SigtranHealthCheckArea.Transport), "health checks should include transport");
    Assert(checks.Any(static check => check.Area == SigtranHealthCheckArea.M3uaSession), "health checks should include M3UA session");
    Assert(checks.Any(static check => check.Area == SigtranHealthCheckArea.Evidence), "health checks should include evidence");
    Assert(checks.Any(static check => check.Area == SigtranHealthCheckArea.Release), "health checks should include release");
}

static void SigtranRollbackPlanDefinesPackageRecoverySteps()
{
    SigtranRollbackPlan plan = SigtranRollbackPlans.CreateDefaultPackageRollback();

    AssertEqual("package-rollback", plan.Id, "rollback plan id");
    AssertEqual(4, plan.Steps.Count, "rollback step count");
    AssertEqual(1, plan.Steps[0].Order, "first rollback step order");
    Assert(plan.Steps.Any(static step => step.Action.Contains("provenance", StringComparison.OrdinalIgnoreCase)), "rollback plan should preserve provenance");
}

static void SigtranMaintenancePolicyGatesProtocolAndTransportChanges()
{
    SigtranMaintenancePolicy policy = SigtranMaintenancePolicies.CreateDefault();

    AssertEqual(TimeSpan.FromDays(7), policy.MinimumNotice, "maintenance minimum notice");
    Assert(policy.RequiresRollbackPlan, "maintenance policy should require rollback plan");
    Assert(policy.RequiresLabValidation(SigtranMaintenanceChangeKind.Protocol), "protocol changes should require lab validation");
    Assert(policy.RequiresLabValidation(SigtranMaintenanceChangeKind.Transport), "transport changes should require lab validation");
    Assert(!policy.RequiresLabValidation(SigtranMaintenanceChangeKind.Documentation), "documentation changes should not require lab validation");
}

static void SigtranSupportHandbookDefinesPublicPrivateAndCommercialChannels()
{
    IReadOnlyList<SigtranSupportRule> rules = SigtranSupportHandbook.GetRules();

    AssertEqual(3, rules.Count, "support rule count");
    Assert(rules.Any(static rule => rule.Channel == SigtranSupportChannel.GitHubIssues), "support should include GitHub issues");
    Assert(rules.Any(static rule => rule.Channel == SigtranSupportChannel.PrivateSecurity && rule.EscalatesIncidents), "support should include private security escalation");
    Assert(rules.Any(static rule => rule.Channel == SigtranSupportChannel.Commercial && rule.EscalatesIncidents), "support should include commercial escalation");
}

static void SigtranOperationsReadinessSeparatesFoundationFromProduction()
{
    SigtranOperationsReadinessReport report = SigtranOperationsReadiness.GetReport();

    Assert(report.FoundationReady, "operations foundation should be ready");
    Assert(!report.CommercialReady, "commercial readiness should still require external gates");
    Assert(!report.ProductionOperationsReady, "production operations should wait for commercial readiness");
}

static void SigtranOperationsCiProfileRequiresOperationsReadiness()
{
    SigtranOperationsCiProfile profile = SigtranOperationsCi.CreateDefault();

    AssertEqual("operations", profile.Name, "operations CI profile name");
    Assert(profile.Commands.Any(static command => command.Contains("dotnet build", StringComparison.Ordinal)), "operations CI should include build");
    Assert(profile.RequiresOperationsReadiness, "operations CI should require operations readiness");
}

static void SigtranOperationsStatusSummarizesOperationsFoundation()
{
    IReadOnlyList<string> capabilities = SigtranOperationsStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranOperationsStatus.CompletedUnitCount, "operations completed unit count");
    AssertEqual(10, capabilities.Count, "operations capability count");
    Assert(capabilities.Contains("operations-ci-profile"), "operations status should include operations CI profile");
    Assert(SigtranOperationsStatus.FoundationReady, SigtranOperationsStatus.Describe());
    Assert(!SigtranOperationsStatus.ProductionOperationsReady, SigtranOperationsStatus.Describe());
}

static void SigtranComplianceCatalogExposesEnterpriseAuditAreas()
{
    IReadOnlyList<SigtranComplianceCapability> capabilities = SigtranCompliance.GetCapabilities();

    AssertEqual(5, capabilities.Count, "compliance capability count");
    Assert(capabilities.Any(capability => capability.Area == SigtranComplianceArea.Audit), "compliance catalog should include audit area");
    Assert(capabilities.Any(capability => capability.Id == "lawful-use-policy"), "compliance catalog should include lawful use policy");
}

static void SigtranAuditEventCatalogMarksEvidenceBearingEvents()
{
    IReadOnlyList<SigtranAuditEventDefinition> events = SigtranAuditEvents.GetDefinitions();

    AssertEqual(5, events.Count, "audit event count");
    Assert(events.Any(item => item.Id == "interop-evidence-promoted" && item.RequiresEvidence), "interop evidence promotion should require evidence");
    Assert(events.Any(item => item.Category == SigtranAuditEventCategory.Compliance), "audit catalog should include compliance review events");
}

static void SigtranEvidenceRetentionPolicyRequiresImmutableRedactedProvenance()
{
    SigtranEvidenceRetentionPolicy policy = SigtranEvidenceRetentionPolicies.CreateCommercialDefault();

    Assert(policy.RetentionPeriod >= TimeSpan.FromDays(365), "retention should be at least one year");
    Assert(policy.RequiresImmutableStorage, "evidence retention should require immutable storage");
    Assert(policy.RequiresTraceRedaction, "evidence retention should require trace redaction");
    Assert(policy.RequiresProvenanceLink, "evidence retention should require provenance links");
    Assert(policy.IsCommercialEvidencePolicy, "evidence retention policy should satisfy commercial evidence controls");
}

static void SigtranLicenseCompliancePolicyTracksApacheAndThirdPartyObligations()
{
    SigtranLicenseCompliancePolicy policy = SigtranLicenseCompliance.CreateCurrentPolicy();

    AssertEqual("Apache-2.0", policy.ProjectLicense, "license compliance project license");
    Assert(policy.RequiresThirdPartyNotices, "license compliance should require third-party notices");
    Assert(policy.RequiresDependencyReview, "license compliance should require dependency review");
    Assert(policy.AllowsCommercialUse, "Apache-2.0 should allow commercial use");
    Assert(policy.IsCommercialReady, "license compliance should be commercial-ready");
}

static void SigtranDataHandlingRulesClassifyConfidentialTraces()
{
    IReadOnlyList<SigtranDataHandlingRule> rules = SigtranDataHandling.GetRules();

    AssertEqual(4, rules.Count, "data handling rule count");
    Assert(rules.Any(rule => rule.Id == "pcap-payload" && rule.Sensitivity == SigtranDataSensitivity.Confidential && rule.RequiresRedaction), "PCAP payloads should be confidential and redacted");
    Assert(rules.Any(rule => rule.Id == "package-metadata" && rule.Sensitivity == SigtranDataSensitivity.Public && !rule.RequiresRedaction), "package metadata should be public");
}

static void SigtranExportControlPolicyRequiresLawfulOperatorAuthorization()
{
    SigtranExportControlPolicy policy = SigtranExportControlPolicies.CreateDefault();

    Assert(policy.RequiresLawfulUseAttestation, "lawful use attestation should be required");
    Assert(policy.RequiresSanctionsScreening, "sanctions screening should be required");
    Assert(policy.RequiresOperatorAuthorization, "operator authorization should be required");
    Assert(policy.HasCommercialControls, "export-control policy should include commercial controls");
}

static void SigtranComplianceReadinessSeparatesFoundationFromCommercialClaims()
{
    SigtranComplianceReadinessReport report = SigtranComplianceReadiness.GetReport();

    Assert(report.FoundationReady, "compliance foundation should be ready");
    Assert(!report.CommercialReady, "compliance readiness should still depend on commercial gates");
    Assert(!report.EnterpriseComplianceReady, "enterprise compliance should not be claimed before commercial readiness");
}

static void SigtranComplianceCiProfileRequiresComplianceReadiness()
{
    SigtranComplianceCiProfile profile = SigtranComplianceCi.CreateDefault();

    AssertEqual("compliance", profile.Name, "compliance CI profile name");
    Assert(profile.Commands.Count >= 3, "compliance CI should reuse official verification commands");
    Assert(profile.RequiresComplianceReadiness, "compliance CI should require compliance readiness");
}

static void SigtranComplianceCommercialGateWaitsForCommercialReadiness()
{
    SigtranComplianceCommercialGateResult result = SigtranComplianceCommercialGate.Evaluate();

    Assert(result.ComplianceFoundationReady, result.Describe());
    Assert(!result.CommercialReady, result.Describe());
    Assert(!result.CanClaimEnterpriseCompliance, result.Describe());
}

static void SigtranComplianceStatusSummarizesComplianceFoundation()
{
    IReadOnlyList<string> capabilities = SigtranComplianceStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranComplianceStatus.CompletedUnitCount, "compliance completed unit count");
    AssertEqual(10, capabilities.Count, "compliance capability count");
    Assert(capabilities.Contains("compliance-ci-profile"), "compliance status should include compliance CI profile");
    Assert(SigtranComplianceStatus.FoundationReady, SigtranComplianceStatus.Describe());
    Assert(!SigtranComplianceStatus.EnterpriseComplianceReady, SigtranComplianceStatus.Describe());
}

static void SigtranPerformanceCatalogExposesBenchmarkCapacityAndResourceAreas()
{
    IReadOnlyList<SigtranPerformanceCapability> capabilities = SigtranPerformance.GetCapabilities();

    AssertEqual(5, capabilities.Count, "performance capability count");
    Assert(capabilities.Any(capability => capability.Area == SigtranPerformanceArea.Benchmarks), "performance catalog should include benchmarks");
    Assert(capabilities.Any(capability => capability.Area == SigtranPerformanceArea.Resources), "performance catalog should include resources");
}

static void SigtranBenchmarkScenariosIncludeLocalAndPeerBenchmarks()
{
    IReadOnlyList<SigtranBenchmarkScenario> scenarios = SigtranBenchmarkScenarios.GetScenarios();

    AssertEqual(5, scenarios.Count, "benchmark scenario count");
    Assert(scenarios.Any(scenario => scenario.Id == "m3ua-data-decode" && !scenario.RequiresExternalPeer), "local decode benchmark should be available");
    Assert(scenarios.Any(scenario => scenario.Id == "external-peer-throughput" && scenario.RequiresExternalPeer), "peer throughput benchmark should require external peer");
}

static void SigtranCapacityProfileDescribesEnterpriseLoadShape()
{
    SigtranCapacityProfile profile = SigtranCapacityProfiles.CreateEnterpriseDefault();

    AssertEqual("enterprise-default", profile.Name, "capacity profile name");
    Assert(profile.MaxAssociations >= 2, "enterprise capacity should include multiple associations");
    Assert(profile.MaxOutboundStreams >= 8, "enterprise capacity should include multiple streams");
    Assert(profile.IsEnterpriseSized, "capacity profile should be enterprise-sized");
}

static void SigtranThroughputTargetsRequireBenchmarkEvidence()
{
    IReadOnlyList<SigtranThroughputTarget> targets = SigtranThroughputTargets.GetTargets();

    AssertEqual(5, targets.Count, "throughput target count");
    Assert(targets.All(target => target.RequiresBenchmarkEvidence), "all throughput targets should require evidence");
    Assert(targets.Any(target => target.Surface == SigtranThroughputSurface.M3uaData && target.MinimumMessagesPerSecond >= 50000), "M3UA DATA target should be high-throughput");
}

static void SigtranLatencyBudgetsDefineP95AndP99Bounds()
{
    IReadOnlyList<SigtranLatencyBudget> budgets = SigtranLatencyBudgets.GetBudgets();

    AssertEqual(4, budgets.Count, "latency budget count");
    Assert(budgets.All(budget => budget.P99Budget > budget.P95Budget), "P99 budgets should be larger than P95 budgets");
    Assert(budgets.Any(budget => budget.Surface == SigtranLatencySurface.TransportLoopback), "transport loopback latency budget should be present");
}

static void SigtranLoadTestPlanDefinesWarmupSustainedAndPeakStages()
{
    SigtranLoadTestPlan plan = SigtranLoadTestPlans.CreateCommercialDefault();

    AssertEqual("commercial-load-test", plan.Name, "load-test plan name");
    AssertEqual(3, plan.Stages.Count, "load-test stage count");
    Assert(plan.RequiresNativeSctp, "commercial load test should require native SCTP");
    Assert(plan.RequiresExternalPeer, "commercial load test should require an external peer");
    AssertEqual(50000, plan.GetPeakMessagesPerSecond(), "load-test peak rate");
}

static void SigtranResourceBudgetRequiresAllocationTracking()
{
    SigtranResourceBudget budget = SigtranResourceBudgets.CreateCommercialDefault();

    AssertEqual(0L, budget.MaxAllocatedBytesPerMessage, "allocation budget");
    Assert(budget.RequiresAllocationTracking, "resource budget should require allocation tracking");
    Assert(budget.IsCommercialBenchmarkBudget, "resource budget should satisfy commercial benchmark controls");
}

static void SigtranPerformanceReadinessSeparatesFoundationFromBenchmarkEvidence()
{
    SigtranPerformanceReadinessReport report = SigtranPerformanceReadiness.GetReport();

    Assert(report.FoundationReady, "performance foundation should be ready");
    Assert(!report.HasBenchmarkEvidence, "performance readiness should wait for real benchmark evidence");
    Assert(!report.ProductionPerformanceReady, "production performance claims should wait for benchmark evidence and commercial readiness");
}

static void SigtranPerformanceCiProfileKeepsBenchmarksOptIn()
{
    SigtranPerformanceCiProfile profile = SigtranPerformanceCi.CreateDefault();

    AssertEqual("performance", profile.Name, "performance CI profile name");
    Assert(profile.Commands.Count >= 3, "performance CI should reuse official verification commands");
    Assert(profile.RequiresPerformanceReadiness, "performance CI should require performance readiness");
    Assert(profile.RequiresOptInBenchmarks, "long-running benchmarks should be opt-in");
}

static void SigtranPerformanceStatusSummarizesPerformanceFoundation()
{
    IReadOnlyList<string> capabilities = SigtranPerformanceStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranPerformanceStatus.CompletedUnitCount, "performance completed unit count");
    AssertEqual(10, capabilities.Count, "performance capability count");
    Assert(capabilities.Contains("performance-ci-profile"), "performance status should include performance CI profile");
    Assert(SigtranPerformanceStatus.FoundationReady, SigtranPerformanceStatus.Describe());
    Assert(!SigtranPerformanceStatus.ProductionPerformanceReady, SigtranPerformanceStatus.Describe());
}

static void SigtranApiSurfaceCatalogExposesProtocolAndGovernanceSurfaces()
{
    IReadOnlyList<SigtranApiSurface> surfaces = SigtranApiSurfaceCatalog.GetSurfaces();

    AssertEqual(6, surfaces.Count, "API surface count");
    Assert(surfaces.Any(surface => surface.Name == "M3UA" && surface.Category == SigtranApiSurfaceCategory.Codec), "M3UA codec surface should be present");
    Assert(surfaces.Any(surface => surface.Name == "CoreUtilities" && surface.Category == SigtranApiSurfaceCategory.Governance), "governance surface should be present");
}

static void SigtranApiStabilityContractsMarkPreStableSurfaces()
{
    IReadOnlyList<SigtranApiStabilityContract> contracts = SigtranApiStability.GetContracts();

    AssertEqual(6, contracts.Count, "API stability contract count");
    Assert(contracts.Any(contract => contract.Surface == "M3UA" && contract.Level == SigtranApiStabilityLevel.Preview), "M3UA should be preview");
    Assert(contracts.Any(contract => contract.Surface == "MAP" && contract.Level == SigtranApiStabilityLevel.Experimental), "MAP should be experimental");
    Assert(contracts.All(contract => contract.AllowsBreakingChangesBeforeStable), "pre-stable contracts should allow breaking changes");
}

static void SigtranApiVersionMatrixSeparatesPreStableAndStableLines()
{
    IReadOnlyList<SigtranApiVersionMatrixEntry> entries = SigtranApiVersionMatrix.GetEntries();

    AssertEqual(2, entries.Count, "API version matrix count");
    Assert(entries.Any(entry => entry.ReleaseLine == "0.x" && entry.AcceptsBreakingChanges), "0.x should accept pre-stable breaking changes");
    Assert(entries.Any(entry => entry.ReleaseLine == "1.x" && !entry.AcceptsBreakingChanges), "1.x should reject breaking changes without major version");
}

static void SigtranDeprecationPolicyRequiresObsoleteMigrationAndReleaseNotes()
{
    SigtranDeprecationPolicy policy = SigtranDeprecationPolicies.CreateStableDefault();

    Assert(policy.MinimumNoticePeriod >= TimeSpan.FromDays(90), "deprecation notice should be at least 90 days");
    Assert(policy.RequiresObsoleteAttribute, "deprecation should require ObsoleteAttribute");
    Assert(policy.RequiresMigrationGuide, "deprecation should require migration guide");
    Assert(policy.RequiresReleaseNotes, "deprecation should require release notes");
    Assert(policy.IsStableLifecyclePolicy, "deprecation policy should satisfy stable lifecycle requirements");
}

static void SigtranMigrationGuidesRequireCodeSamples()
{
    IReadOnlyList<SigtranMigrationGuideEntry> entries = SigtranMigrationGuides.GetEntries();

    AssertEqual(3, entries.Count, "migration guide count");
    Assert(entries.All(entry => entry.RequiresCodeSamples), "migration guides should require code samples");
    Assert(entries.Any(entry => entry.Id == "prestable-to-1.0"), "prestable to 1.0 migration guide should be planned");
}

static void SigtranBreakingChangeReviewRequiresBaselineMigrationAndApproval()
{
    SigtranBreakingChangeReviewPolicy policy = SigtranBreakingChangeReview.CreateDefault();

    Assert(policy.RequiresApiBaselineDiff, "breaking-change review should require API baseline diff");
    Assert(policy.RequiresMigrationGuide, "breaking-change review should require migration guide");
    Assert(policy.RequiresMaintainerApproval, "breaking-change review should require maintainer approval");
    Assert(policy.RequiresMajorVersionAfterStable, "stable breaking changes should require major version");
    Assert(policy.IsCommercialApiGovernanceReady, "breaking-change review should be commercial governance ready");
}

static void SigtranPublicApiBaselineCoversKnownSurfaces()
{
    SigtranPublicApiBaselineManifest manifest = SigtranPublicApiBaseline.CreateCurrent();

    AssertEqual("prestable-public-api", manifest.Name, "public API baseline name");
    Assert(manifest.RequiresDiffReview, "public API baseline should require diff review");
    Assert(manifest.CoversKnownSurfaces, "public API baseline should cover known surfaces");
}

static void SigtranApiLifecycleReadinessSeparatesFoundationFromStableClaims()
{
    SigtranApiLifecycleReadinessReport report = SigtranApiLifecycleReadiness.GetReport();

    Assert(report.FoundationReady, "API lifecycle foundation should be ready");
    Assert(!report.CommercialReady, "API lifecycle readiness should still depend on commercial gates");
    Assert(!report.StableApiLifecycleReady, "stable API lifecycle should not be claimed before commercial readiness");
}

static void SigtranApiLifecycleCiProfileRequiresPublicApiDiffReview()
{
    SigtranApiLifecycleCiProfile profile = SigtranApiLifecycleCi.CreateDefault();

    AssertEqual("api-lifecycle", profile.Name, "API lifecycle CI profile name");
    Assert(profile.Commands.Count >= 3, "API lifecycle CI should reuse official verification commands");
    Assert(profile.RequiresApiLifecycleReadiness, "API lifecycle CI should require readiness");
    Assert(profile.RequiresPublicApiDiffReview, "API lifecycle CI should require public API diff review");
}

static void SigtranApiLifecycleStatusSummarizesApiLifecycleFoundation()
{
    IReadOnlyList<string> capabilities = SigtranApiLifecycleStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranApiLifecycleStatus.CompletedUnitCount, "API lifecycle completed unit count");
    AssertEqual(10, capabilities.Count, "API lifecycle capability count");
    Assert(capabilities.Contains("api-lifecycle-ci-profile"), "API lifecycle status should include API lifecycle CI profile");
    Assert(SigtranApiLifecycleStatus.FoundationReady, SigtranApiLifecycleStatus.Describe());
    Assert(!SigtranApiLifecycleStatus.StableApiLifecycleReady, SigtranApiLifecycleStatus.Describe());
}

static void SigtranConfigurationSchemaExposesRequiredTransportRoutingAndSecurityKeys()
{
    IReadOnlyList<SigtranConfigurationSchemaField> fields = SigtranConfigurationSchema.GetFields();

    AssertEqual(7, fields.Count, "configuration schema field count");
    Assert(fields.Any(field => field.Area == SigtranConfigurationSchemaArea.Transport && field.Required), "transport required fields should be present");
    Assert(fields.Any(field => field.Key == "sigtran.m3ua.routingContexts" && field.Required), "routing context key should be required");
    Assert(fields.Any(field => field.Key == "sigtran.security.secretProvider" && field.Required), "secret provider key should be required");
}

static void SigtranConfigurationValidationReportsMissingRequiredKeys()
{
    SigtranConfigurationValidationResult invalid = SigtranConfigurationValidation.ValidateRequiredKeys("production", ["sigtran.transport.kind"]);
    string[] requiredKeys = SigtranConfigurationSchema.GetFields()
        .Where(field => field.Required)
        .Select(field => field.Key)
        .ToArray();
    SigtranConfigurationValidationResult valid = SigtranConfigurationValidation.ValidateRequiredKeys("production", requiredKeys);

    Assert(!invalid.IsValid, "partial configuration should be invalid");
    Assert(invalid.MissingRequiredKeys.Contains("sigtran.security.secretProvider"), "missing secret provider should be reported");
    Assert(valid.IsValid, "complete required configuration should be valid");
}

static void SigtranEnvironmentMatrixSeparatesDevelopmentLabAndProductionRequirements()
{
    IReadOnlyList<SigtranEnvironmentMatrixEntry> entries = SigtranEnvironmentMatrix.GetEntries();

    AssertEqual(3, entries.Count, "environment matrix count");
    Assert(entries.Any(entry => entry.Environment == SigtranRuntimeEnvironment.Development && !entry.RequiresNativeSctp), "development should not require native SCTP");
    Assert(entries.Any(entry => entry.Environment == SigtranRuntimeEnvironment.InteropLab && entry.RequiresEvidenceRoot), "interop lab should require evidence root");
    Assert(entries.Any(entry => entry.Environment == SigtranRuntimeEnvironment.Production && entry.RequiresExternalSecretProvider), "production should require external secret provider");
}

static void SigtranSecretPolicyRejectsProductionPlaintextSecrets()
{
    SigtranSecretPolicy policy = SigtranSecretPolicies.CreateDefault();

    Assert(policy.AllowsPlainTextInDevelopment, "development plaintext should be allowed for local workflows");
    Assert(!policy.AllowsPlainTextInProduction, "production plaintext secrets should be rejected");
    Assert(policy.RequiresExternalProviderInProduction, "production should require external secret provider");
    Assert(policy.RequiresRotationPlan, "secret policy should require rotation plan");
    Assert(policy.IsProductionSafe, "secret policy should be production safe");
}

static void SigtranTransportConfigurationRequiresPpidStreamsAndReconnectPolicy()
{
    SigtranTransportConfiguration configuration = SigtranTransportConfigurations.CreateNativeSctpDefault();

    AssertEqual("native-sctp", configuration.Kind, "transport configuration kind");
    Assert(configuration.RequiresPpid, "transport configuration should require PPID");
    Assert(configuration.RequiresStreamPolicy, "transport configuration should require stream policy");
    Assert(configuration.RequiresReconnectPolicy, "transport configuration should require reconnect policy");
    Assert(configuration.IsSigtranReady, "transport configuration should be SIGTRAN-ready");
}

static void SigtranRoutingConfigurationRequiresRouteValidationAndAmbiguityRejection()
{
    SigtranRoutingConfiguration configuration = SigtranRoutingConfigurations.CreateEnterpriseDefault();

    Assert(configuration.RequiresRoutingContext, "routing configuration should require Routing Context");
    Assert(configuration.RequiresNetworkAppearancePolicy, "routing configuration should require Network Appearance policy");
    Assert(configuration.RequiresRouteTableValidation, "routing configuration should require route table validation");
    Assert(configuration.RequiresAmbiguityRejection, "routing configuration should reject ambiguity");
    Assert(configuration.IsEnterpriseReady, "routing configuration should be enterprise-ready");
}

static void SigtranConfigurationReadinessSeparatesFoundationFromCommercialClaims()
{
    SigtranConfigurationReadinessReport report = SigtranConfigurationReadiness.GetReport();

    Assert(report.FoundationReady, "configuration foundation should be ready");
    Assert(!report.CommercialReady, "configuration readiness should still depend on commercial gates");
    Assert(!report.ProductionConfigurationReady, "production configuration should not be claimed before commercial readiness");
}

static void SigtranConfigurationCiProfileRejectsProductionPlaintextSecrets()
{
    SigtranConfigurationCiProfile profile = SigtranConfigurationCi.CreateDefault();

    AssertEqual("configuration", profile.Name, "configuration CI profile name");
    Assert(profile.Commands.Count >= 3, "configuration CI should reuse official verification commands");
    Assert(profile.RequiresConfigurationReadiness, "configuration CI should require configuration readiness");
    Assert(profile.RejectsProductionPlainTextSecrets, "configuration CI should reject production plaintext secrets");
}

static void SigtranConfigurationCommercialGateWaitsForCommercialReadiness()
{
    SigtranConfigurationCommercialGateResult result = SigtranConfigurationCommercialGate.Evaluate();

    Assert(result.ConfigurationFoundationReady, result.Describe());
    Assert(result.ProductionSecretsSafe, result.Describe());
    Assert(!result.CommercialReady, result.Describe());
    Assert(!result.CanClaimProductionConfiguration, result.Describe());
}

static void SigtranConfigurationStatusSummarizesConfigurationFoundation()
{
    IReadOnlyList<string> capabilities = SigtranConfigurationStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranConfigurationStatus.CompletedUnitCount, "configuration completed unit count");
    AssertEqual(10, capabilities.Count, "configuration capability count");
    Assert(capabilities.Contains("configuration-ci-profile"), "configuration status should include configuration CI profile");
    Assert(SigtranConfigurationStatus.FoundationReady, SigtranConfigurationStatus.Describe());
    Assert(!SigtranConfigurationStatus.ProductionConfigurationReady, SigtranConfigurationStatus.Describe());
}

static void SigtranNativeSctpLabScenariosRequireLinuxVerification()
{
    IReadOnlyList<SigtranNativeSctpLabScenario> scenarios = SigtranNativeSctpLabScenarios.GetScenarios();

    AssertEqual(4, scenarios.Count, "native SCTP lab scenario count");
    Assert(scenarios.All(scenario => scenario.RequiresLinux), "all native SCTP lab scenarios should require Linux");
    Assert(scenarios.Any(scenario => scenario.Kind == SigtranNativeSctpLabScenarioKind.ExternalPeerTraffic && scenario.RequiresExternalPeer), "external peer traffic scenario should require peer");
}

static void SigtranNativeSctpLabArtifactManifestValidatesRequiredArtifacts()
{
    SigtranNativeSctpLabScenario scenario = SigtranNativeSctpLabScenarios.GetScenarios()[1];
    SigtranNativeSctpLabArtifactManifest manifest = new(scenario.Id);
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PacketCapture, "artifacts/loopback.pcapng"));
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.SdkTrace, "artifacts/sdk-trace.log"));
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PlatformReport, "artifacts/health.json"));

    Assert(manifest.Satisfies(scenario), "manifest should satisfy loopback scenario");
}

static void SigtranNativeSctpLabRunPlanIncludesExternalPeerTraffic()
{
    SigtranNativeSctpLabRunPlan plan = SigtranNativeSctpLabRunPlans.CreateDefault();

    AssertEqual("native-sctp-linux-verification", plan.Name, "native SCTP lab plan name");
    AssertEqual(4, plan.Scenarios.Count, "native SCTP lab plan scenario count");
    Assert(plan.RequiresRootOrCapabilities, "native SCTP lab should require root or capabilities");
    Assert(plan.RequiresPacketCapture, "native SCTP lab should require packet capture");
    Assert(plan.IncludesExternalPeer, "native SCTP lab should include external peer scenario");
}

static void SigtranNativeSctpLabCommandsRequireLinuxAndLksctpTools()
{
    SigtranNativeSctpLabCommandSet commands = SigtranNativeSctpLabCommands.CreateDefault();

    Assert(commands.RequiresLinux, "native SCTP lab commands should require Linux");
    Assert(commands.RequiresLksctpTools, "native SCTP lab commands should require lksctp-tools");
    Assert(commands.Commands.Any(command => command.Contains("SIGTRAN_NATIVE_SCTP_LAB=1", StringComparison.Ordinal)), "native SCTP lab commands should enable lab variable");
}

static void SigtranNativeSctpLabRunReportIdentifiesPassingEvidence()
{
    SigtranNativeSctpLabScenario scenario = SigtranNativeSctpLabScenarios.GetScenarios()[0];
    SigtranNativeSctpLabArtifactManifest manifest = new(scenario.Id);
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PlatformReport, "artifacts/platform-probe.json"));
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PlatformReport, "artifacts/kernel.txt"));
    SigtranNativeSctpLabRunReport report = new(scenario, manifest, SigtranNativeSctpLabRunStatus.Passed, "Linux 6.x", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddMinutes(1));

    Assert(report.HasPassingEvidence, report.Describe());
}

static void SigtranNativeSctpLabEvidenceRegistryRequiresAllScenarios()
{
    SigtranNativeSctpLabEvidenceRegistry registry = new();
    SigtranNativeSctpLabScenario scenario = SigtranNativeSctpLabScenarios.GetScenarios()[0];
    SigtranNativeSctpLabArtifactManifest manifest = new(scenario.Id);
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PlatformReport, "artifacts/platform-probe.json"));
    manifest.Add(new SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind.PlatformReport, "artifacts/kernel.txt"));
    registry.Add(new SigtranNativeSctpLabRunReport(scenario, manifest, SigtranNativeSctpLabRunStatus.Passed, "Linux 6.x", DateTimeOffset.UnixEpoch));

    Assert(!registry.HasCompletePassingEvidence(), "partial native SCTP evidence should not unlock production verification");
}

static void SigtranNativeSctpLabReadinessSeparatesFoundationFromEvidence()
{
    SigtranNativeSctpLabReadinessReport report = SigtranNativeSctpLabReadiness.GetReport();

    Assert(report.FoundationReady, "native SCTP lab foundation should be ready");
    Assert(!report.HasCompletePassingEvidence, "native SCTP lab should wait for real evidence");
    Assert(!report.ProductionReady, "native SCTP lab production readiness should wait for evidence");
}

static void SigtranNativeSctpLabCiProfileIsOptInAndLinuxOnly()
{
    SigtranNativeSctpLabCiProfile profile = SigtranNativeSctpLabCi.CreateDefault();

    AssertEqual("SIGTRAN_NATIVE_SCTP_LAB", profile.EnableVariable, "native SCTP lab enable variable");
    Assert(profile.RequiresLinuxRunner, "native SCTP lab CI should require Linux runner");
    Assert(!profile.IsEnabled(new Dictionary<string, string>()), "native SCTP lab CI should be opt-in");
    Assert(profile.IsEnabled(new Dictionary<string, string> { ["SIGTRAN_NATIVE_SCTP_LAB"] = "true" }), "native SCTP lab CI should enable from variable");
}

static void SigtranNativeSctpLabCommercialGateWaitsForCompleteEvidence()
{
    SigtranNativeSctpLabCommercialGateResult result = SigtranNativeSctpLabCommercialGate.Evaluate();

    Assert(result.LabFoundationReady, result.Describe());
    Assert(!result.HasCompletePassingEvidence, result.Describe());
    Assert(!result.CanClaimNativeSctpProduction, result.Describe());
}

static void SigtranNativeSctpLabVerificationStatusSummarizesFoundation()
{
    IReadOnlyList<string> capabilities = SigtranNativeSctpLabVerificationStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranNativeSctpLabVerificationStatus.CompletedUnitCount, "native SCTP lab verification completed unit count");
    AssertEqual(10, capabilities.Count, "native SCTP lab verification capability count");
    Assert(capabilities.Contains("native-sctp-lab-ci-profile"), "native SCTP lab verification status should include native SCTP lab CI profile");
    Assert(SigtranNativeSctpLabVerificationStatus.FoundationReady, SigtranNativeSctpLabVerificationStatus.Describe());
    Assert(!SigtranNativeSctpLabVerificationStatus.NativeSctpProductionVerified, SigtranNativeSctpLabVerificationStatus.Describe());
}

static void SigtranExternalPeerInteropEnvironmentRequiresLinuxSctpPeerAndPacketCapture()
{
    SigtranExternalPeerInteropEnvironment environment = SigtranExternalPeerInteropEnvironments.CreateDefault();

    AssertEqual("external-sigtran-peer-linux-lab", environment.Name, "external peer environment name");
    Assert(environment.RequiresLinux, "external peer interop should require Linux");
    Assert(environment.RequiresNativeSctp, "external peer interop should require native SCTP");
    Assert(environment.RequiresExternalPeer, "external peer interop should require peer");
    Assert(environment.RequiresPacketCapture, "external peer interop should require packet capture");
    Assert(environment.RequiresSdkTrace, "external peer interop should require SDK trace capture");
    Assert(environment.RequiresPeerConfiguration, "external peer interop should require peer configuration capture");
    Assert(environment.RequiresPeerLog, "external peer interop should require peer log capture");
    AssertEqual("artifacts/external-peer", environment.ArtifactRoot, "external peer artifact root");
    Assert(environment.RequiredTools.Contains("tcpdump"), "external peer environment should require packet capture tooling");
    Assert(environment.CanProduceCommercialArtifacts, "external peer environment should produce commercial artifacts");
}

static void SigtranExternalPeerInteropConfigurationExposesAspToSgDefaults()
{
    SigtranExternalPeerInteropConfiguration configuration = SigtranExternalPeerInteropConfigurations.CreateDefaultAspToSg();

    AssertEqual("sigtran-net-asp", configuration.AssociationName, "external peer association name");
    AssertEqual("sigtran-net-as", configuration.ApplicationServerName, "external peer AS name");
    AssertEqual(1u, configuration.RoutingContext, "external peer routing context");
    AssertEqual("override", configuration.TrafficMode, "external peer traffic mode");
    Assert(configuration.IsAspToSgReady, "external peer ASP-to-SG configuration should be ready");
}

static void SigtranExternalPeerTraceExpectationsCoverAspLifecycleAndData()
{
    SigtranExternalPeerInteropTraceExpectations expectations = SigtranExternalPeerInteropTraceExpectationsCatalog.CreateAspToSg();

    AssertEqual("external-peer-m3ua-asp-to-sg", expectations.ScenarioId, "external peer trace scenario");
    Assert(expectations.CoversAspLifecycle, "external peer trace expectations should cover ASP lifecycle");
    Assert(expectations.RequiresDataTransfer, "external peer trace expectations should require DATA transfer");
    Assert(expectations.ExpectedMessages.Contains("DATA"), "external peer trace expectations should include DATA");
}

static void SigtranExternalPeerArtifactManifestRequiresTracePeerConfigLogsAndComparison()
{
    SigtranExternalPeerInteropArtifactManifest manifest = CreateCompleteExternalPeerManifest();

    AssertEqual(5, manifest.Snapshot().Count, "external peer artifact count");
    Assert(manifest.IsComplete, "external peer artifact manifest should be complete");
    Assert(manifest.AllArtifactsHaveDigests, "external peer artifact manifest should require digests");
    Assert(manifest.IsReviewReady, "external peer artifact manifest should be review ready");

    SigtranExternalPeerInteropArtifactManifest incomplete = new();
    incomplete.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.PacketCapture, "artifacts/external-peer/peer.pcapng"));
    Assert(!incomplete.IsComplete, "incomplete external peer manifest should not be complete");
    Assert(!incomplete.IsReviewReady, "incomplete external peer manifest should not be review ready");
    Assert(incomplete.GetMissingRequiredKinds().Contains(SigtranExternalPeerInteropArtifactKind.ComparisonReport), "missing required kinds should include comparison report");
}

static void SigtranExternalPeerRunPlanIsExecutableWithDefaultContracts()
{
    SigtranExternalPeerInteropRunPlan plan = SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg();

    AssertEqual("external-peer-m3ua-asp-to-sg", plan.Template.Scenario.Id, "external peer scenario id");
    AssertEqual("external-sigtran-sg", plan.Template.PeerProfile.Id, "external peer profile id");
    Assert(plan.CommandSet.IsCommercialLabCommandSet, "external peer run plan should include commercial lab commands");
    Assert(plan.IsExecutable, "external peer run plan should be executable");
}

static void SigtranExternalPeerCommandsRequirePeerAndPacketCapture()
{
    SigtranExternalPeerInteropCommandSet commands = SigtranExternalPeerInteropCommands.CreateDefault();

    Assert(commands.RequiresExternalPeer, "external peer commands should require an external peer");
    Assert(commands.RequiresPacketCapture, "external peer commands should require packet capture");
    Assert(commands.RequiresSdkTrace, "external peer commands should require SDK trace capture");
    Assert(commands.RequiresComparisonReport, "external peer commands should require comparison report generation");
    Assert(commands.RequiredEnvironmentVariables.Contains("SIGTRAN_EXTERNAL_PEER_PACKAGE"), "external peer commands should require selected peer package env var");
    Assert(commands.Commands.Any(command => command.Contains("SIGTRAN_EXTERNAL_PEER_ID", StringComparison.Ordinal)), "external peer commands should select peer id through env");
    Assert(commands.Commands.Any(command => command.Contains("SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT", StringComparison.Ordinal)), "external peer commands should write under artifact root");
    Assert(commands.IsCommercialLabCommandSet, "external peer command set should be commercial lab ready");
}

static void SigtranExternalPeerRunReportIdentifiesPassingEvidence()
{
    SigtranExternalPeerInteropRunReport report = new(
        SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg(),
        CreateCompleteExternalPeerManifest(),
        SigtranExternalPeerInteropRunStatus.Passed,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch.AddMinutes(3));

    Assert(report.HasPassingEvidence, "external peer run report should identify passing evidence");
    Assert(report.HasCommercialReviewReadyEvidence, "external peer run report should identify review-ready evidence");

    SigtranExternalPeerInteropEvidenceRegistry registry = new();
    registry.Add(report);
    Assert(registry.HasPassingAspToSgEvidence, "external peer registry should identify passing evidence");
    Assert(registry.HasCommercialReviewReadyEvidence, "external peer registry should identify review-ready evidence");
}

static void SigtranExternalPeerEvidenceRegistryStartsEmpty()
{
    SigtranExternalPeerInteropEvidenceRegistry registry = SigtranExternalPeerInteropEvidence.CreateCurrentRegistry();

    AssertEqual(0, registry.Snapshot().Count, "current external peer evidence count");
    Assert(!registry.HasPassingAspToSgEvidence, "current external peer evidence should be empty until real artifacts are promoted");
}

static void SigtranExternalPeerReadinessSeparatesFoundationFromEvidence()
{
    SigtranExternalPeerInteropReadinessReport report = SigtranExternalPeerInteropReadiness.GetReport();

    Assert(report.FoundationReady, "external peer interop foundation should be ready");
    Assert(!report.HasPassingEvidence, "external peer interop should wait for real evidence");
    Assert(!report.Verified, "external peer interop should not be verified without evidence");
}

static void SigtranExternalPeerCommercialReadinessAggregatesSelectionLabAndEvidence()
{
    SigtranExternalPeerCommercialReadinessReport current = SigtranExternalPeerCommercialReadiness.CreateCurrent();
    Assert(!current.FoundationReady, current.Describe());
    Assert(!current.CommercialInteropReady, current.Describe());
    Assert(current.Blockers.Contains("maintained-peer-selection-required"), "current commercial readiness should require maintained peer selection");
    Assert(current.Blockers.Contains("external-peer-review-ready-evidence-required"), "current commercial readiness should require review-ready evidence");

    SigtranExternalPeerInteropEvidenceRegistry registry = new();
    registry.Add(new SigtranExternalPeerInteropRunReport(
        SigtranExternalPeerInteropRunPlans.CreateDefaultAspToSg(),
        CreateCompleteExternalPeerManifest(),
        SigtranExternalPeerInteropRunStatus.Passed,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch.AddMinutes(1)));

    IReadOnlyList<string> criteria = SigtranMaintainedPeerSelectionPolicy.CreateDefault()
        .GetCriteria()
        .Select(static criterion => criterion.Id)
        .ToArray();

    SigtranExternalPeerCommercialReadinessReport ready = SigtranExternalPeerCommercialReadiness.Create(
        SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway(),
        criteria,
        registry);

    Assert(ready.FoundationReady, ready.Describe());
    Assert(ready.CommercialInteropReady, ready.Describe());
    AssertEqual(0, ready.Blockers.Count, "ready external peer commercial blocker count");
}

static void SigtranExternalPeerStatusSummarizesExecutionFoundation()
{
    IReadOnlyList<string> capabilities = SigtranExternalPeerInteropStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranExternalPeerInteropStatus.CompletedUnitCount, "external peer completed unit count");
    AssertEqual(10, capabilities.Count, "external peer capability count");
    Assert(capabilities.Contains("external-peer-ci-profile"), "external peer status should include CI profile");
    Assert(SigtranExternalPeerInteropStatus.FoundationReady, SigtranExternalPeerInteropStatus.Describe());
    Assert(!SigtranExternalPeerInteropStatus.Verified, SigtranExternalPeerInteropStatus.Describe());
}

static void SigtranCommercialRoadmapRealignmentStatusSummarizesPackageNeutralCompletion()
{
    IReadOnlyList<string> capabilities = SigtranCommercialRoadmapRealignmentStatus.GetCompletedCapabilities();
    IReadOnlyList<string> publicNames = SigtranCommercialRoadmapRealignmentStatus.GetPublicContractNames();
    IReadOnlyList<string> guardCategories = SigtranCommercialRoadmapRealignmentStatus.GetPackageSpecificNameGuardCategories();
    SigtranCommercialRoadmapRealignmentReport report = SigtranCommercialRoadmapRealignmentStatus.GetReport();

    AssertEqual(10, SigtranCommercialRoadmapRealignmentStatus.CompletedUnitCount, "commercial roadmap realignment completed unit count");
    AssertEqual(10, capabilities.Count, "commercial roadmap realignment capability count");
    AssertEqual(3, guardCategories.Count, "commercial roadmap realignment guard category count");
    Assert(capabilities.Contains("public-label-migration"), "commercial roadmap realignment should include public label migration");
    Assert(publicNames.All(name => name.Contains("external", StringComparison.OrdinalIgnoreCase)
        || name.Contains("commercial", StringComparison.OrdinalIgnoreCase)
        || name.Contains("maintained", StringComparison.OrdinalIgnoreCase)
        || name.Contains("evidence", StringComparison.OrdinalIgnoreCase)
        || name.Contains("package-neutral", StringComparison.OrdinalIgnoreCase)
        || name.Contains("public-label", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "documentation", StringComparison.OrdinalIgnoreCase)), "public realignment contract names should use package-neutral domains");
    Assert(report.PackageNeutralNamingReady, report.Describe());
    Assert(report.MaintainedPeerPolicyReady, report.Describe());
    Assert(report.ExternalPeerExecutionFoundationReady, report.Describe());
    Assert(report.CommercialGateAligned, report.Describe());
    Assert(report.FoundationReady, report.Describe());
    Assert(!report.CommercialReleaseReady, report.Describe());
    Assert(report.Blockers.Contains("maintained-peer-selection-required"), "current realignment report should require maintained peer selection evidence");
    Assert(report.Blockers.Contains("external-peer-review-ready-evidence-required"), "current realignment report should require review-ready external peer evidence");
    Assert(SigtranCommercialRoadmapRealignmentStatus.Describe().Contains(SigtranCommercialRoadmapRealignmentStatus.StatusLabel, StringComparison.Ordinal), "realignment description should include the status label");
}

static SigtranExternalPeerInteropArtifactManifest CreateCompleteExternalPeerManifest()
{
    SigtranExternalPeerInteropArtifactManifest manifest = new();
    manifest.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.PacketCapture, "artifacts/external-peer/peer.pcapng", "SHA256-001"));
    manifest.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.SdkTrace, "artifacts/external-peer/sdk-trace.log", "SHA256-002"));
    manifest.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.PeerConfiguration, "artifacts/external-peer/peer-config.txt", "SHA256-003"));
    manifest.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.PeerLog, "artifacts/external-peer/peer-log.txt", "SHA256-004"));
    manifest.Add(new SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind.ComparisonReport, "artifacts/external-peer/comparison-report.md", "SHA256-005"));
    return manifest;
}

static void SigtranProtocolInteropVectorCatalogCoversSccpTcapAndMap()
{
    IReadOnlyList<SigtranProtocolInteropVector> vectors = SigtranProtocolInteropVectorCatalog.GetVectors();

    AssertEqual(6, vectors.Count, "protocol interop vector count");
    Assert(vectors.Any(vector => vector.Surface == SigtranProtocolInteropSurface.Sccp), "SCCP vector should exist");
    Assert(vectors.Any(vector => vector.Surface == SigtranProtocolInteropSurface.Tcap), "TCAP vector should exist");
    Assert(vectors.Any(vector => vector.Surface == SigtranProtocolInteropSurface.MapSms), "MAP SMS vector should exist");
    Assert(vectors.All(vector => vector.RequiresExternalReference), "all protocol interop vectors should require external references");
}

static void SigtranProtocolInteropReferencesRequireTraceValidation()
{
    IReadOnlyList<SigtranProtocolInteropReference> references = SigtranProtocolInteropReferences.GetReferences();

    AssertEqual(3, references.Count, "protocol interop reference count");
    Assert(references.All(reference => reference.RequiresTraceValidation), "all protocol interop references should require trace validation");
}

static void SigtranProtocolInteropArtifactManifestRequiresReferenceSdkAndComparison()
{
    SigtranProtocolInteropArtifactManifest manifest = CreateCompleteProtocolInteropManifest();

    AssertEqual(3, manifest.Snapshot().Count, "protocol interop artifact count");
    Assert(manifest.IsComplete, "protocol interop manifest should be complete");
}

static void SigtranProtocolInteropComparisonRulesAreCommercialValidationReady()
{
    SigtranProtocolInteropComparisonRuleSet rules = SigtranProtocolInteropComparisonRules.CreateDefault();

    Assert(rules.RequiresByteExactEncoding, "protocol interop should require byte-exact encoding");
    Assert(rules.RequiresDecodedFieldComparison, "protocol interop should require decoded field comparison");
    Assert(rules.RequiresTraceOrderValidation, "protocol interop should require trace order validation");
    Assert(rules.AllowsOperatorSpecificExtensions, "protocol interop should allow operator-specific extensions");
    Assert(rules.IsCommercialValidationReady, "protocol interop rules should be commercial validation ready");
}

static void SigtranProtocolInteropRunPlanIsExecutableWithExternalVectors()
{
    SigtranProtocolInteropRunPlan plan = SigtranProtocolInteropRunPlans.CreateDefault();

    Assert(plan.IsExecutable, "protocol interop run plan should be executable");
    AssertEqual(6, plan.Vectors.Count, "protocol interop run plan vector count");
    Assert(plan.RequiresExternalVectors, "protocol interop run plan should require external vectors");
}

static void SigtranProtocolInteropCommandsRequireVectorRootAndReports()
{
    SigtranProtocolInteropCommandSet commands = SigtranProtocolInteropCommands.CreateDefault();

    Assert(commands.RequiresExternalVectorRoot, "protocol interop commands should require vector root");
    Assert(commands.RequiresComparisonReports, "protocol interop commands should require comparison reports");
    Assert(commands.Commands.Any(command => command.Contains("SIGTRAN_PROTOCOL_VECTOR_ROOT", StringComparison.Ordinal)), "protocol interop commands should mention vector root variable");
}

static void SigtranProtocolInteropRunReportIdentifiesPassingEvidence()
{
    SigtranProtocolInteropVector vector = SigtranProtocolInteropVectorCatalog.GetVectors()[0];
    SigtranProtocolInteropRunReport report = new(
        vector,
        CreateCompleteProtocolInteropManifest(),
        SigtranProtocolInteropRunStatus.Passed,
        new DateTimeOffset(2026, 6, 19, 8, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 6, 19, 8, 30, 0, TimeSpan.Zero));

    Assert(report.HasPassingEvidence, "protocol interop run report should have passing evidence");
}

static void SigtranProtocolInteropEvidenceRegistryStartsEmpty()
{
    SigtranProtocolInteropEvidenceRegistry registry = SigtranProtocolInteropEvidence.CreateCurrentRegistry();

    AssertEqual(0, registry.Snapshot().Count, "protocol interop evidence count");
    Assert(!registry.HasCompletePassingEvidence, "protocol interop current evidence should not be complete");
}

static void SigtranProtocolInteropReadinessSeparatesFoundationFromEvidence()
{
    SigtranProtocolInteropReadinessReport report = SigtranProtocolInteropReadiness.GetReport();

    Assert(report.FoundationReady, "protocol interop foundation should be ready");
    Assert(!report.HasCompletePassingEvidence, "protocol interop evidence should not be complete");
    Assert(!report.Verified, "protocol interop should not be verified without complete evidence");
}

static void SigtranProtocolInteropStatusSummarizesVectorFoundation()
{
    IReadOnlyList<string> capabilities = SigtranProtocolInteropStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranProtocolInteropStatus.CompletedUnitCount, "protocol interop completed unit count");
    AssertEqual(10, capabilities.Count, "protocol interop capability count");
    Assert(capabilities.Contains("protocol-ci-profile"), "protocol interop status should include CI profile");
    Assert(SigtranProtocolInteropStatus.FoundationReady, SigtranProtocolInteropStatus.Describe());
    Assert(!SigtranProtocolInteropStatus.Verified, SigtranProtocolInteropStatus.Describe());
}

static SigtranProtocolInteropArtifactManifest CreateCompleteProtocolInteropManifest()
{
    SigtranProtocolInteropArtifactManifest manifest = new();
    manifest.Add(new SigtranProtocolInteropArtifact(SigtranProtocolInteropArtifactKind.ReferenceVector, "artifacts/protocol-vectors/reference.ber"));
    manifest.Add(new SigtranProtocolInteropArtifact(SigtranProtocolInteropArtifactKind.SdkVector, "artifacts/protocol-vectors/sdk.ber"));
    manifest.Add(new SigtranProtocolInteropArtifact(SigtranProtocolInteropArtifactKind.ComparisonReport, "artifacts/protocol-vectors/comparison.md"));
    return manifest;
}

static void SigtranCommercialEvidenceRequirementsCoverProductionClaimAreas()
{
    IReadOnlyList<SigtranCommercialEvidenceRequirement> requirements = SigtranCommercialEvidenceRequirements.GetRequirements();

    AssertEqual(5, requirements.Count, "commercial evidence requirement count");
    Assert(requirements.Any(requirement => requirement.Area == SigtranCommercialEvidenceArea.NativeSctp), "native SCTP evidence requirement should exist");
    Assert(requirements.Any(requirement => requirement.Area == SigtranCommercialEvidenceArea.ExternalPeerInterop), "external peer evidence requirement should exist");
    Assert(requirements.Any(requirement => requirement.Area == SigtranCommercialEvidenceArea.ProtocolInterop), "protocol evidence requirement should exist");
    Assert(requirements.Any(requirement => requirement.Area == SigtranCommercialEvidenceArea.ReleaseProvenance), "release provenance evidence requirement should exist");
    Assert(requirements.Any(requirement => requirement.Area == SigtranCommercialEvidenceArea.PackageArtifacts), "package artifact evidence requirement should exist");
    Assert(requirements.All(requirement => requirement.RequiredArtifactKinds.Count > 0), "all commercial evidence requirements should have artifacts");
}

static void SigtranCommercialEvidenceManifestSatisfiesRequirementsWithDigests()
{
    SigtranCommercialEvidenceManifest manifest = CreateCompleteCommercialEvidenceManifest();
    IReadOnlyList<SigtranCommercialEvidenceRequirement> requirements = SigtranCommercialEvidenceRequirements.GetRequirements();

    Assert(manifest.SatisfiesAll(requirements), "complete commercial evidence manifest should satisfy all requirements");
    Assert(manifest.AllArtifactsHaveDigests(), "complete commercial evidence manifest should have digests");
}

static void SigtranCommercialEvidenceBundleKeepsEmptyDossierIncomplete()
{
    SigtranCommercialEvidenceBundle bundle = SigtranCommercialEvidenceBundles.CreateEmpty("1.0.0");

    AssertEqual("1.0.0", bundle.ReleaseVersion, "commercial evidence bundle version");
    Assert(!bundle.HasCompleteArtifacts, "empty commercial evidence bundle should not have complete artifacts");
    Assert(!bundle.HasDigestCoverage, "empty commercial evidence bundle should not have digest coverage");
    Assert(!bundle.IsComplete, bundle.Describe());
}

static void SigtranCommercialEvidenceBundleCompletesWithRetainedArtifacts()
{
    SigtranCommercialEvidenceBundle bundle = new(
        "1.0.0",
        SigtranCommercialEvidenceRequirements.GetRequirements(),
        CreateCompleteCommercialEvidenceManifest());

    Assert(bundle.HasCompleteArtifacts, "commercial evidence bundle should have complete artifacts");
    Assert(bundle.HasDigestCoverage, "commercial evidence bundle should have digest coverage");
    Assert(bundle.IsComplete, bundle.Describe());
}

static void SigtranCommercialEvidenceGateReportsMissingCurrentEvidence()
{
    SigtranCommercialEvidenceBundle bundle = SigtranCommercialEvidenceBundles.CreateEmpty("1.0.0");
    SigtranCommercialEvidenceGateResult result = SigtranCommercialEvidenceGate.Evaluate(
        bundle,
        nativeSctpVerified: false,
        externalPeerInteropVerified: false,
        protocolInteropVerified: false,
        releaseGovernanceReady: false);

    Assert(!result.CanClaimCommercialEvidence, "empty commercial evidence should not support claims");
    Assert(result.Reasons.Contains("commercial-evidence-artifacts-incomplete"), "commercial evidence gate should report incomplete artifacts");
    Assert(result.Reasons.Contains("native-sctp-evidence-required"), "commercial evidence gate should report native SCTP evidence");
    Assert(result.Reasons.Contains("external-peer-evidence-required"), "commercial evidence gate should report external peer evidence");
    Assert(result.Reasons.Contains("protocol-vector-evidence-required"), "commercial evidence gate should report protocol vector evidence");
}

static void SigtranCommercialEvidenceGateAllowsCompleteVerifiedDossier()
{
    SigtranCommercialEvidenceBundle bundle = new(
        "1.0.0",
        SigtranCommercialEvidenceRequirements.GetRequirements(),
        CreateCompleteCommercialEvidenceManifest());
    SigtranCommercialEvidenceGateResult result = SigtranCommercialEvidenceGate.Evaluate(
        bundle,
        nativeSctpVerified: true,
        externalPeerInteropVerified: true,
        protocolInteropVerified: true,
        releaseGovernanceReady: true);

    Assert(result.CanClaimCommercialEvidence, result.Describe());
    AssertEqual(0, result.Reasons.Count, "complete commercial evidence gate reason count");
}

static void SigtranCommercialEvidenceReadinessSeparatesFoundationFromClaims()
{
    SigtranCommercialEvidenceReadinessReport report = SigtranCommercialEvidenceReadiness.GetReport();

    Assert(report.FoundationReady, "commercial evidence foundation should be ready");
    Assert(!report.CurrentEvidenceReady, "current commercial evidence should not be ready");
    Assert(!report.CommercialEvidenceReady, "commercial evidence should not support current production claims");
}

static void SigtranCommercialEvidenceCiProfileRequiresRetainedBundle()
{
    SigtranCommercialEvidenceCiProfile profile = SigtranCommercialEvidenceCi.CreateDefault();

    AssertEqual("SIGTRAN_COMMERCIAL_EVIDENCE", profile.EnableVariable, "commercial evidence CI enable variable");
    AssertEqual("SIGTRAN_COMMERCIAL_EVIDENCE_ROOT", profile.BundleRootVariable, "commercial evidence CI root variable");
    Assert(profile.RequiresEvidenceBundle, "commercial evidence CI should require retained bundle");
    Assert(profile.Commands.Any(command => command.Contains("sigtran-evidence-verify", StringComparison.Ordinal)), "commercial evidence CI should include evidence verification command");
    Assert(profile.IsEnabled(new Dictionary<string, string> { ["SIGTRAN_COMMERCIAL_EVIDENCE"] = "true" }), "commercial evidence CI should be enabled by true");
}

static void SigtranCommercialEvidenceStatusSummarizesDossierFoundation()
{
    IReadOnlyList<string> capabilities = SigtranCommercialEvidenceStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranCommercialEvidenceStatus.CompletedUnitCount, "commercial evidence completed unit count");
    AssertEqual(10, capabilities.Count, "commercial evidence capability count");
    Assert(capabilities.Contains("commercial-evidence-gate"), "commercial evidence status should include gate");
    Assert(SigtranCommercialEvidenceStatus.FoundationReady, SigtranCommercialEvidenceStatus.Describe());
    Assert(!SigtranCommercialEvidenceStatus.CommercialEvidenceReady, SigtranCommercialEvidenceStatus.Describe());
}

static void SigtranSupplyChainAutomationPlanWiresSbomSigningProvenanceAndEvidence()
{
    SigtranSupplyChainAutomationPlan plan = SigtranSupplyChainAutomation.CreateDefaultPlan();

    AssertEqual("supply-chain-default", plan.Id, "supply-chain plan id");
    AssertEqual("artifacts/supply-chain", plan.ArtifactRoot, "supply-chain artifact root");
    Assert(plan.IsExecutable, "supply-chain automation plan should be executable");
    Assert(plan.SbomPlan.IsRequiredForCommercialRelease, "supply-chain plan should require SBOM");
    Assert(plan.SigningPlan.HasSigningMaterialReferences, "supply-chain plan should have signing references");
}

static void SigtranSupplyChainCommandsExposeOrderedReleaseSecuritySteps()
{
    IReadOnlyList<string> commands = SigtranSupplyChainAutomation.CreateDefaultPlan().GetCommands();

    AssertEqual(6, commands.Count, "supply-chain command count");
    Assert(commands[1].Contains("sbom", StringComparison.OrdinalIgnoreCase), "second supply-chain command should generate SBOM");
    Assert(commands[2].Contains("nuget sign", StringComparison.OrdinalIgnoreCase), "third supply-chain command should sign package");
    Assert(commands[4].Contains("provenance", StringComparison.OrdinalIgnoreCase), "fifth supply-chain command should create provenance");
    Assert(commands[5].Contains("evidence", StringComparison.OrdinalIgnoreCase), "sixth supply-chain command should verify evidence");
}

static void SigtranSupplyChainArtifactManifestValidatesRequiredArtifacts()
{
    SigtranSupplyChainArtifactManifest manifest = CreateCompleteSupplyChainManifest();

    AssertEqual(5, manifest.Snapshot().Count, "supply-chain artifact count");
    Assert(manifest.HasRequiredArtifacts, "supply-chain manifest should have required artifacts");
    Assert(manifest.AllArtifactsHaveDigests, "supply-chain manifest should have artifact digests");
}

static void SigtranSupplyChainGateBlocksIncompletePromotionEvidence()
{
    SigtranSupplyChainGateResult result = SigtranSupplyChainGate.Evaluate(
        SigtranSupplyChainAutomation.CreateDefaultPlan(),
        new SigtranSupplyChainArtifactManifest(),
        SigtranReleaseProvenanceFactory.Create("abcdef0", "artifacts/release-manifest.json"),
        commercialEvidenceReady: false);

    Assert(!result.CanPromote, "empty supply-chain evidence should not promote");
    Assert(result.Reasons.Contains("supply-chain-artifacts-incomplete"), "supply-chain gate should report incomplete artifacts");
    Assert(result.Reasons.Contains("supply-chain-digests-incomplete"), "supply-chain gate should report incomplete digests");
    Assert(result.Reasons.Contains("commercial-evidence-required"), "supply-chain gate should require commercial evidence");
}

static void SigtranSupplyChainGateAllowsCompleteVerifiedPromotion()
{
    SigtranSupplyChainGateResult result = SigtranSupplyChainGate.Evaluate(
        SigtranSupplyChainAutomation.CreateDefaultPlan(),
        CreateCompleteSupplyChainManifest(),
        SigtranReleaseProvenanceFactory.Create("abcdef0", "artifacts/release-manifest.json"),
        commercialEvidenceReady: true);

    Assert(result.CanPromote, result.Describe());
    AssertEqual(0, result.Reasons.Count, "complete supply-chain gate reasons");
}

static void SigtranSupplyChainReadinessSeparatesFoundationFromPromotion()
{
    SigtranSupplyChainReadinessReport report = SigtranSupplyChainReadiness.GetReport();

    Assert(report.FoundationReady, "supply-chain foundation should be ready");
    Assert(!report.HasCurrentPromotionEvidence, "current supply-chain promotion evidence should be absent");
    Assert(!report.PromotionReady, "supply-chain promotion should not be ready without evidence");
}

static void SigtranSupplyChainCiProfileRequiresSigningSecrets()
{
    SigtranSupplyChainCiProfile profile = SigtranSupplyChainCi.CreateDefault();

    AssertEqual("SIGTRAN_SUPPLY_CHAIN", profile.EnableVariable, "supply-chain CI enable variable");
    AssertEqual("SIGTRAN_SUPPLY_CHAIN_ARTIFACT_ROOT", profile.ArtifactRootVariable, "supply-chain CI artifact root variable");
    Assert(profile.RequiresSigningSecrets, "supply-chain CI should require signing secrets");
    Assert(profile.RequiredSecrets.Contains("SIGNING_CERTIFICATE"), "supply-chain CI should require signing certificate");
    Assert(profile.IsEnabled(new Dictionary<string, string> { ["SIGTRAN_SUPPLY_CHAIN"] = "1" }), "supply-chain CI should be enabled by 1");
}

static void SigtranSupplyChainStatusSummarizesAutomationFoundation()
{
    IReadOnlyList<string> capabilities = SigtranSupplyChainStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranSupplyChainStatus.CompletedUnitCount, "supply-chain completed unit count");
    AssertEqual(10, capabilities.Count, "supply-chain capability count");
    Assert(capabilities.Contains("supply-chain-ci-profile"), "supply-chain status should include CI profile");
    Assert(SigtranSupplyChainStatus.FoundationReady, SigtranSupplyChainStatus.Describe());
    Assert(!SigtranSupplyChainStatus.PromotionReady, SigtranSupplyChainStatus.Describe());
}

static void SigtranSupplyChainReferencesAlignWithSbomAndSigningPlans()
{
    SigtranSupplyChainAutomationPlan plan = SigtranSupplyChainAutomation.CreateDefaultPlan();

    AssertEqual(SigtranSbom.CreateDefaultPlan().OutputPath, plan.SbomPlan.OutputPath, "supply-chain SBOM output path");
    AssertEqual(SigtranPackageSigning.CreateDefaultPlan().TimestampAuthorityUrl, plan.SigningPlan.TimestampAuthorityUrl, "supply-chain timestamp URL");
    Assert(plan.Steps.Any(static step => step.RequiresSecret), "supply-chain plan should include secret-backed step");
}

static void SigtranSupplyChainArtifactDigestsAreMandatoryForPromotion()
{
    SigtranSupplyChainArtifactManifest manifest = new();
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.Sbom, "artifacts/supply-chain/sbom.spdx.json"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.Signature, "artifacts/supply-chain/package.sig", "SHA256-001"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.TimestampReceipt, "artifacts/supply-chain/timestamp.tsr", "SHA256-002"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.ProvenanceAttestation, "artifacts/supply-chain/provenance.json", "SHA256-003"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.VerificationReport, "artifacts/supply-chain/verification.md", "SHA256-004"));

    Assert(manifest.HasRequiredArtifacts, "supply-chain manifest should have required artifacts");
    Assert(!manifest.AllArtifactsHaveDigests, "supply-chain manifest without all digests should not promote");
}

static void SigtranReleaseWorkflowPlanIncludesReleaseTriggersAndStages()
{
    SigtranReleaseWorkflowPlan plan = SigtranReleaseWorkflows.CreateCommercialReleasePlan();

    AssertEqual("release", plan.WorkflowName, "release workflow name");
    AssertEqual("10.0.x", plan.DotNetVersion, "release workflow .NET version");
    Assert(plan.Triggers.Contains(SigtranReleaseWorkflowTrigger.ManualDispatch), "release workflow should support manual dispatch");
    Assert(plan.Triggers.Contains(SigtranReleaseWorkflowTrigger.VersionTag), "release workflow should support version tags");
    AssertEqual(9, plan.Stages.Count, "release workflow stage count");
    Assert(plan.IsRenderable, "release workflow should be renderable");
}

static void SigtranReleaseWorkflowRequiresSupplyChainEvidenceAndPublishSecrets()
{
    SigtranReleaseWorkflowPlan plan = SigtranReleaseWorkflows.CreateCommercialReleasePlan();
    IReadOnlyList<string> secrets = plan.GetRequiredSecrets();

    Assert(plan.RequiresSupplyChain, "release workflow should require supply-chain automation");
    Assert(plan.RequiresCommercialEvidence, "release workflow should require commercial evidence");
    Assert(plan.HasPublishStage, "release workflow should include publish stage");
    Assert(secrets.Contains("NUGET_API_KEY"), "release workflow should require NuGet API key");
    Assert(secrets.Contains("SIGNING_CERTIFICATE"), "release workflow should require signing certificate");
}

static void SigtranReleaseWorkflowReadinessReportsConcreteWorkflowFile()
{
    SigtranReleaseWorkflowReadinessReport report = SigtranReleaseWorkflowReadiness.GetReport();

    Assert(report.ContractReady, "release workflow contract should be ready");
    Assert(report.HasWorkflowFile, "release workflow file should be marked ready");
    Assert(report.OrchestrationReady, "release workflow orchestration foundation should be ready");
}

static void SigtranReleaseWorkflowStatusSummarizesOrchestrationFoundation()
{
    IReadOnlyList<string> capabilities = SigtranReleaseWorkflowStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranReleaseWorkflowStatus.CompletedUnitCount, "release workflow completed unit count");
    AssertEqual(10, capabilities.Count, "release workflow capability count");
    Assert(capabilities.Contains("promotion-gate"), "release workflow status should include promotion gate");
    Assert(capabilities.Contains("workflow-yaml-validation"), "release workflow status should include YAML validation");
    Assert(SigtranReleaseWorkflowStatus.ContractReady, SigtranReleaseWorkflowStatus.Describe());
    Assert(SigtranReleaseWorkflowStatus.OrchestrationReady, SigtranReleaseWorkflowStatus.Describe());
}

static void SigtranReleaseWorkflowFileContractTracksConcreteWorkflowFile()
{
    SigtranReleaseWorkflowFileContract contract = SigtranReleaseWorkflowFiles.CreateDefault();

    AssertEqual(".github/workflows/release.yml", contract.Path, "release workflow file path");
    AssertEqual("release", contract.WorkflowName, "release workflow file name");
    AssertEqual("release", contract.RequiredJobName, "release workflow job name");
    Assert(contract.RequiredStageNames.Contains("Supply Chain"), "release workflow file should include supply-chain stage");
    Assert(contract.RequiredStageNames.Contains("Commercial Evidence"), "release workflow file should include commercial evidence stage");
    Assert(contract.IsValidationReady, "release workflow file contract should be validation ready");
}

static void SigtranReleaseWorkflowValidationAcceptsConcreteWorkflowYaml()
{
    string yaml = File.ReadAllText(Path.Combine(".github", "workflows", "release.yml"));
    SigtranReleaseWorkflowValidationResult result = SigtranReleaseWorkflowValidation.ValidateYaml(yaml);

    Assert(result.IsValid, result.Describe());
    AssertEqual(0, result.MissingItems.Count, "release workflow validation missing item count");
}

static void SigtranReleasePublishGuardBlocksAccidentalPublication()
{
    SigtranReleasePublishGuardResult result = SigtranReleasePublishGuard.Evaluate(new(
        isManualDispatch: false,
        publishRequested: false,
        isVersionTag: false,
        hasNuGetApiKey: false));

    Assert(!result.CanPublish, "publish guard should block accidental publication");
    Assert(result.Reasons.Contains("publish-not-requested"), "publish guard should require explicit request");
    Assert(result.Reasons.Contains("manual-dispatch-required"), "publish guard should require manual dispatch");
    Assert(result.Reasons.Contains("version-tag-required"), "publish guard should require version tag");
    Assert(result.Reasons.Contains("nuget-api-key-required"), "publish guard should require NuGet API key");
}

static void SigtranReleasePublishGuardAllowsIntentionalTaggedPublication()
{
    SigtranReleasePublishGuardResult result = SigtranReleasePublishGuard.Evaluate(new(
        isManualDispatch: true,
        publishRequested: true,
        isVersionTag: true,
        hasNuGetApiKey: true));

    Assert(result.CanPublish, result.Describe());
    AssertEqual(0, result.Reasons.Count, "publish guard reason count");
}

static void SigtranReleaseWorkflowArtifactRulesRetainPackagesAndEvidence()
{
    IReadOnlyList<SigtranReleaseWorkflowArtifactRule> rules = SigtranReleaseWorkflowArtifacts.GetDefaultRules();

    AssertEqual(4, rules.Count, "release workflow artifact rule count");
    Assert(rules.Any(static rule => rule.Name == "nuget-packages"), "release workflow should retain NuGet packages");
    Assert(rules.Any(static rule => rule.Name == "supply-chain"), "release workflow should retain supply-chain artifacts");
    Assert(SigtranReleaseWorkflowArtifacts.RetainsCommercialEvidence(), "release workflow should retain commercial evidence");
    Assert(rules.All(static rule => rule.RetentionDays >= 90), "release workflow artifacts should have audit-friendly retention");
}

static void SigtranReleaseWorkflowPermissionsUseLeastPrivilege()
{
    SigtranReleaseWorkflowPermissionSet permissions = SigtranReleaseWorkflowPermissions.CreateDefault();
    string yaml = File.ReadAllText(Path.Combine(".github", "workflows", "release.yml"));

    Assert(permissions.IsLeastPrivilege, "release workflow permissions should be least privilege");
    Assert(yaml.Contains("contents: read", StringComparison.Ordinal), "release workflow YAML should keep contents read-only");
    Assert(yaml.Contains("id-token: write", StringComparison.Ordinal), "release workflow YAML should allow OIDC token");
}

static void SigtranReleaseWorkflowConcurrencyPreventsOverlappingReleases()
{
    SigtranReleaseWorkflowConcurrencyPolicy policy = SigtranReleaseWorkflowConcurrency.CreateDefault();
    string yaml = File.ReadAllText(Path.Combine(".github", "workflows", "release.yml"));

    Assert(policy.PreventsOverlappingReleaseRuns, "release workflow concurrency should prevent overlapping runs");
    Assert(yaml.Contains("group: release-${{ github.ref }}", StringComparison.Ordinal), "release workflow YAML should include concurrency group");
    Assert(yaml.Contains("cancel-in-progress: false", StringComparison.Ordinal), "release workflow YAML should avoid cancelling active release");
}

static void SigtranReleaseWorkflowEnvironmentExposesSupplyChainAndEvidenceVariables()
{
    IReadOnlyList<SigtranReleaseWorkflowEnvironmentVariable> variables = SigtranReleaseWorkflowEnvironment.GetRequiredVariables();
    string yaml = File.ReadAllText(Path.Combine(".github", "workflows", "release.yml"));

    AssertEqual(5, variables.Count, "release workflow environment variable count");
    Assert(variables.Any(static variable => variable.Name == "SIGTRAN_SUPPLY_CHAIN_ARTIFACT_ROOT"), "release workflow should include supply-chain artifact root");
    Assert(variables.Any(static variable => variable.Name == "SIGTRAN_COMMERCIAL_EVIDENCE_ROOT"), "release workflow should include commercial evidence root");
    Assert(SigtranReleaseWorkflowEnvironment.ArePresentInYaml(yaml), "release workflow YAML should contain required environment variables");
}

static void SigtranReleasePromotionGateBlocksIncompleteReleaseEvidence()
{
    SigtranReleasePromotionGateResult result = SigtranReleasePromotionGate.Evaluate(
        SigtranReleasePublishGuard.Evaluate(new(false, false, false, false)),
        workflowOrchestrationReady: SigtranReleaseWorkflowStatus.OrchestrationReady,
        supplyChainPromotionReady: false,
        commercialEvidenceReady: false);

    Assert(!result.CanPromote, "release promotion should be blocked without evidence");
    Assert(result.Reasons.Contains("publish-not-requested"), "release promotion should include publish guard reasons");
    Assert(result.Reasons.Contains("supply-chain-promotion-required"), "release promotion should require supply-chain promotion");
    Assert(result.Reasons.Contains("commercial-evidence-required"), "release promotion should require commercial evidence");
}

static void SigtranReleasePromotionGateAllowsCompleteReleaseEvidence()
{
    SigtranReleasePromotionGateResult result = SigtranReleasePromotionGate.Evaluate(
        SigtranReleasePublishGuard.Evaluate(new(true, true, true, true)),
        workflowOrchestrationReady: true,
        supplyChainPromotionReady: true,
        commercialEvidenceReady: true);

    Assert(result.CanPromote, result.Describe());
    AssertEqual(0, result.Reasons.Count, "release promotion reason count");
}

static void SigtranReleaseVersionPolicyAcceptsSemVerTags()
{
    SigtranReleaseVersionPolicy policy = SigtranReleaseVersionPolicies.CreateDefault();

    Assert(policy.CoversPublicationLanes, "release version policy should cover prerelease and stable lanes");
    Assert(policy.IsValidTag("v1.0.0-alpha.1"), "release version policy should accept prerelease tag");
    Assert(policy.IsValidTag("v1.0.0"), "release version policy should accept stable tag");
    Assert(!policy.IsValidTag("release-1.0.0"), "release version policy should reject unexpected tag prefix");
    Assert(!policy.IsValidPackageVersion("1.0"), "release version policy should reject incomplete version");
    AssertEqual(SigtranReleaseVersionLane.Prerelease, policy.GetLane("1.0.0-alpha.1"), "prerelease lane");
    AssertEqual(SigtranReleaseVersionLane.Stable, policy.GetLane("1.0.0"), "stable lane");
}

static void SigtranNuGetMetadataContractMatchesProjectFile()
{
    SigtranNuGetMetadataContract contract = SigtranNuGetMetadata.CreateDefaultContract();
    string projectXml = File.ReadAllText(Path.Combine("src", "Sigtran.NET", "Sigtran.NET.csproj"));
    IReadOnlyList<string> missing = contract.GetMissingProperties(projectXml);

    Assert(contract.IsPublicationReady, "NuGet metadata contract should cover publication-critical metadata");
    AssertEqual(0, missing.Count, "missing NuGet metadata count");
    Assert(contract.Requirements.Any(static requirement => requirement.PropertyName == "PackageLicenseExpression"), "metadata contract should require license");
    Assert(contract.Requirements.Any(static requirement => requirement.PropertyName == "SymbolPackageFormat"), "metadata contract should require symbol package format");
}

static void SigtranPackageLayoutExposesNupkgAndSymbols()
{
    SigtranPackageLayout layout = SigtranPackageLayouts.CreateDefault();
    IReadOnlyList<SigtranPackageArtifactPath> artifacts = layout.GetArtifactPaths();

    Assert(layout.IncludesRequiredArtifacts, "package layout should include package and symbol artifacts");
    AssertEqual(2, artifacts.Count, "package layout artifact count");
    Assert(artifacts.Any(static artifact => artifact.Path.EndsWith("Sigtran.NET.1.0.0.nupkg", StringComparison.Ordinal)), "package layout should include nupkg");
    Assert(artifacts.Any(static artifact => artifact.Path.EndsWith("Sigtran.NET.1.0.0.snupkg", StringComparison.Ordinal)), "package layout should include snupkg");
}

static void SigtranNuGetPublishPlansSeparateDryRunAndPublish()
{
    SigtranNuGetPublishPlan dryRun = SigtranNuGetPublishPlans.CreateDryRun();
    SigtranNuGetPublishPlan publish = SigtranNuGetPublishPlans.CreatePublish();

    Assert(dryRun.IsDryRunSafe, "dry-run publish plan should not upload packages");
    Assert(!dryRun.RequiresApiKey, "dry-run publish plan should not require API key");
    Assert(publish.IsPublishCapable, "publish plan should include NuGet push");
    Assert(publish.RequiresApiKey, "publish plan should require API key");
    AssertEqual(dryRun.Source, publish.Source, "NuGet source should match between dry-run and publish");
}

static void SigtranPublicationCredentialPolicyRequiresCommercialSecrets()
{
    SigtranPublicationCredentialPolicy policy = SigtranPublicationCredentials.CreateDefaultPolicy();
    HashSet<string> available = ["NUGET_API_KEY"];
    IReadOnlyList<string> missing = policy.GetMissingSecrets(available);

    Assert(policy.RequiresCommercialSecrets, "publication credential policy should require commercial secrets");
    AssertEqual(3, policy.Credentials.Count, "publication credential count");
    Assert(missing.Contains("SIGNING_CERTIFICATE"), "credential policy should require signing certificate");
    Assert(missing.Contains("SIGNING_CERTIFICATE_PASSWORD"), "credential policy should require signing certificate password");
}

static void SigtranPublicationChannelPolicySeparatesPrereleaseAndStable()
{
    SigtranPublishChannel alpha = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Alpha);
    SigtranPublishChannel stable = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Stable);

    SigtranPublicationChannelDecision alphaDecision = SigtranPublicationChannelPolicy.Evaluate(alpha, "1.0.0-alpha.1", commercialReadiness: false);
    SigtranPublicationChannelDecision stableBlocked = SigtranPublicationChannelPolicy.Evaluate(stable, "1.0.0-alpha.1", commercialReadiness: false);
    SigtranPublicationChannelDecision stableAllowed = SigtranPublicationChannelPolicy.Evaluate(stable, "1.0.0", commercialReadiness: true);

    Assert(alphaDecision.Allowed, "alpha channel should accept prerelease versions without commercial readiness");
    Assert(!stableBlocked.Allowed, "stable channel should block prerelease and missing commercial readiness");
    Assert(stableBlocked.Reasons.Contains("channel-version-mismatch"), "stable channel should reject prerelease version");
    Assert(stableBlocked.Reasons.Contains("commercial-readiness-required"), "stable channel should require commercial readiness");
    Assert(stableAllowed.Allowed, "stable channel should allow stable version after commercial readiness");
}

static void SigtranPackageIntegrityManifestRequiresPackageDigests()
{
    SigtranPackageIntegrityManifest incomplete = new();
    incomplete.Add(new SigtranPackageIntegrityEntry(SigtranPackageArtifactKind.Package, "src/Sigtran.NET/bin/Release/Sigtran.NET.1.0.0.nupkg", "SHA256-NUPKG"));
    SigtranPackageIntegrityManifest complete = SigtranPackageIntegrityManifest.CreateCompleteSample();

    Assert(!incomplete.IsComplete, "package integrity manifest should require symbol package digest");
    Assert(complete.IsComplete, "package integrity manifest should be complete with package and symbol digests");
    AssertEqual(2, complete.Entries.Count, "package integrity entry count");
}

static void SigtranPublicationEvidenceManifestRequiresIntegrityAndReleaseEvidence()
{
    SigtranPublicationEvidenceManifest incomplete = new(
        "1.0.0",
        SigtranPublishChannelKind.Stable,
        packageIntegrityComplete: true,
        supplyChainPromotionReady: false,
        commercialEvidenceReady: true);
    SigtranPublicationEvidenceManifest complete = SigtranPublicationEvidenceManifest.CreateCompleteSample();

    Assert(!incomplete.IsComplete, "publication evidence should require supply-chain evidence");
    Assert(complete.IsComplete, "publication evidence should be complete with integrity and release evidence");
    AssertEqual(SigtranPublishChannelKind.Stable, complete.Channel, "publication evidence channel");
}

static void SigtranPublicationGateBlocksIncompletePublishReadiness()
{
    SigtranPublishChannel stable = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Stable);
    SigtranPublicationGateResult result = SigtranPublicationGate.Evaluate(
        SigtranReleasePublishGuard.Evaluate(new(false, false, false, false)),
        SigtranPublicationChannelPolicy.Evaluate(stable, "1.0.0-alpha.1", commercialReadiness: false),
        SigtranPublicationCredentials.CreateDefaultPolicy(),
        new HashSet<string>(),
        new SigtranPublicationEvidenceManifest("1.0.0-alpha.1", SigtranPublishChannelKind.Stable, true, false, false),
        metadataReady: false,
        layoutReady: false);

    Assert(!result.CanPublish, "publication gate should block incomplete readiness");
    Assert(result.Reasons.Contains("publish-not-requested"), "publication gate should include publish guard reasons");
    Assert(result.Reasons.Contains("publication-evidence-required"), "publication gate should require publication evidence");
    Assert(result.Reasons.Contains("nuget-metadata-required"), "publication gate should require metadata");
    Assert(result.Reasons.Any(static reason => reason.StartsWith("missing-secret:", StringComparison.Ordinal)), "publication gate should require secrets");
}

static void SigtranPublicationGateAllowsCompletePublishReadiness()
{
    SigtranPublishChannel stable = SigtranPublishChannels.GetChannels().Single(static channel => channel.Kind == SigtranPublishChannelKind.Stable);
    HashSet<string> secrets = ["NUGET_API_KEY", "SIGNING_CERTIFICATE", "SIGNING_CERTIFICATE_PASSWORD"];
    SigtranPublicationGateResult result = SigtranPublicationGate.Evaluate(
        SigtranReleasePublishGuard.Evaluate(new(true, true, true, true)),
        SigtranPublicationChannelPolicy.Evaluate(stable, "1.0.0", commercialReadiness: true),
        SigtranPublicationCredentials.CreateDefaultPolicy(),
        secrets,
        SigtranPublicationEvidenceManifest.CreateCompleteSample(),
        metadataReady: true,
        layoutReady: true);

    Assert(result.CanPublish, result.Describe());
    AssertEqual(0, result.Reasons.Count, "publication gate reason count");
}

static void SigtranPackagePublicationStatusSummarizesReadinessFoundation()
{
    SigtranPackagePublicationReadinessReport readiness = SigtranPackagePublicationStatus.GetReadiness();
    IReadOnlyList<string> capabilities = SigtranPackagePublicationStatus.GetCompletedCapabilities();

    AssertEqual(10, SigtranPackagePublicationStatus.CompletedUnitCount, "package publication completed unit count");
    AssertEqual(10, capabilities.Count, "package publication capability count");
    Assert(capabilities.Contains("publication-gate"), "package publication status should include publication gate");
    Assert(readiness.FoundationReady, "package publication foundation should be ready");
    Assert(!readiness.PublicationReady, "real package publication should remain blocked without live evidence and credentials");
    Assert(SigtranPackagePublicationStatus.FoundationReady, SigtranPackagePublicationStatus.Describe());
    Assert(!SigtranPackagePublicationStatus.PublicationReady, SigtranPackagePublicationStatus.Describe());
}

static void SigtranCommercialReleaseExecutionEvidenceTracksPassedAndBlockedArtifacts()
{
    SigtranCommercialReleaseEvidenceManifest manifest = SigtranCommercialReleaseEvidenceManifest.CreateCurrentSample();

    Assert(manifest.HasPassedArea(SigtranCommercialReleaseEvidenceArea.LinuxSctp), "commercial release evidence should include Linux SCTP evidence");
    Assert(manifest.HasBlockers, "commercial release evidence should record the external peer blocker");
    Assert(manifest.HasDigestCoverage, "commercial release evidence should require digest coverage for passed artifacts");
    Assert(!manifest.SupportsCommercialPromotion, "commercial release evidence should not promote while external peer is blocked");
    Assert(manifest.Artifacts.Any(static artifact => artifact.Kind == SigtranCommercialReleaseEvidenceKind.BlockerReport), "commercial release evidence should include blocker report artifact");
}

static void SigtranLinuxSctpEvidenceRecordsPassingSmokeCapture()
{
    SigtranLinuxSctpCaptureSummary summary = SigtranLinuxSctpEvidence.CreateCurrentSmokeSummary();

    Assert(summary.IsPassingSmokeEvidence, summary.Describe());
    AssertEqual(10, summary.PacketCount, "Linux SCTP packet count");
    AssertEqual(10, summary.SctpPacketCount, "Linux SCTP SCTP-packet count");
    Assert(summary.FileSizeBytes > 24, "Linux SCTP PCAP should be larger than a header-only capture");
    Assert(summary.HasAssociationHandshake, "Linux SCTP capture should include association handshake");
    Assert(summary.HasDataExchange, "Linux SCTP capture should include DATA chunks");
    Assert(summary.HasCleanShutdown, "Linux SCTP capture should include clean shutdown");
}

static void SigtranExternalPeerInteropBlockerEvidenceRecordsRetainedFailureContext()
{
    SigtranExternalPeerInteropBlocker blocker = SigtranExternalPeerInteropBlockerEvidence.CreateCurrentBlocker();

    Assert(blocker.BlocksInteropPromotion, "external peer blocker should prevent interop promotion");
    Assert(blocker.EnvironmentName.Contains("Ubuntu 22.04.1 VM", StringComparison.OrdinalIgnoreCase), "external peer blocker should record the environment");
    Assert(blocker.LogPath.EndsWith("openss7-configure.log", StringComparison.Ordinal), "external peer blocker should retain configure log path");
    Assert(blocker.ObservedFailure.Contains("open_softirq", StringComparison.OrdinalIgnoreCase), blocker.Describe());
    Assert(blocker.RequiredAction.Contains("Retest", StringComparison.OrdinalIgnoreCase), "external peer blocker should describe the next action");
}

static void SigtranCommercialReleaseArtifactDossierTracksRetainedAndMissingArtifacts()
{
    SigtranCommercialReleaseArtifactDossier dossier = SigtranCommercialReleaseArtifactDossiers.CreateCurrent();
    IReadOnlyList<SigtranCommercialReleaseArtifactRecord> artifacts = dossier.Snapshot();

    AssertEqual(5, artifacts.Count, "commercial release artifact dossier artifact count");
    Assert(dossier.HasInteropArtifactSet == false, "commercial release artifact dossier should not count missing trace and comparison as retained");
    Assert(dossier.HasDigestCoverage, "retained commercial release artifacts should include digest placeholders until real digests are materialized");
    Assert(dossier.IsReviewReady == false, "commercial release artifact dossier should not be review-ready while trace and comparison are missing");
    Assert(artifacts.Any(static artifact => artifact.Kind == SigtranCommercialReleaseEvidenceKind.Trace && artifact.Retention == SigtranCommercialReleaseArtifactRetention.Missing), "commercial release artifact dossier should track missing SDK trace");
    Assert(artifacts.Any(static artifact => artifact.Kind == SigtranCommercialReleaseEvidenceKind.ComparisonReport && artifact.Retention == SigtranCommercialReleaseArtifactRetention.Missing), "commercial release artifact dossier should track missing comparison report");
}

static void SigtranSbomExecutionEvidenceRecordsGeneratedSpdxOutput()
{
    SigtranSbomExecutionEvidence evidence = SigtranSbomExecution.CreateFromGeneratedDigest(
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        packageFileCount: 2);

    Assert(evidence.IsReviewReady, "generated SBOM evidence should be review-ready");
    AssertEqual(SigtranSbomFormat.SpdxJson, evidence.Format, "generated SBOM evidence format");
    AssertEqual("artifacts/sbom/Sigtran.NET.spdx.json", evidence.OutputPath, "generated SBOM evidence output path");
    AssertEqual(64, evidence.Sha256.Length, "generated SBOM evidence SHA-256 length");
    AssertEqual(2, evidence.PackageFileCount, "generated SBOM package count");
}

static void SigtranPackageSigningExecutionEvidenceRecordsVerificationBlocker()
{
    SigtranPackageSigningExecutionEvidence evidence = SigtranPackageSigningExecution.CreateFromSignedPackageDigest(
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

    Assert(evidence.SigningSucceeded, "package signing should have produced a signed package");
    Assert(evidence.VerificationPassed == false, "package signing evidence should record verification failure");
    Assert(evidence.Timestamped == false, "package signing evidence should require timestamping before promotion");
    Assert(evidence.TrustedCertificate == false, "package signing evidence should require trusted certificate chain before promotion");
    Assert(evidence.SupportsCommercialPromotion == false, "package signing evidence should block commercial promotion until verification passes");
}

static void SigtranProvenanceExecutionEvidenceRecordsPackageAndSbomDigests()
{
    SigtranProvenanceExecutionEvidence evidence = SigtranProvenanceExecution.CreateFromRetainedDigests(
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        "0123456789abcdef0123456789abcdef01234567",
        "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
        "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210");

    Assert(evidence.IsReviewReady, "provenance evidence should be review-ready with retained package and SBOM digests");
    Assert(evidence.OutputPath.EndsWith(".provenance.json", StringComparison.OrdinalIgnoreCase), "provenance evidence should point to JSON attestation");
    AssertEqual(64, evidence.PackageSha256.Length, "provenance package digest length");
    AssertEqual(64, evidence.SbomSha256.Length, "provenance SBOM digest length");
}

static void SigtranBenchmarkExecutionEvidenceRecordsSmokeWorkloadLimits()
{
    SigtranBenchmarkExecutionEvidence evidence = SigtranBenchmarkExecution.CreateSmokeBenchmark(
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        durationMilliseconds: 500,
        passedChecks: 200);

    Assert(evidence.SmokeOnly, "benchmark evidence should record smoke-only status");
    Assert(evidence.SupportsCommercialPerformancePromotion == false, "smoke benchmark should not support commercial performance promotion");
    AssertEqual(200, evidence.PassedChecks, "benchmark passed check count");
    AssertEqual(64, evidence.ReportSha256.Length, "benchmark report digest length");
}

static void SigtranPublicApiBaselineEvidenceRecordsGeneratedMemberBaseline()
{
    SigtranPublicApiBaselineEvidence evidence = SigtranPublicApiBaselineEvidenceFactory.CreateFromRetainedBaseline(
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
        memberCount: 300);

    Assert(evidence.IsReviewReady, "public API baseline evidence should be review-ready");
    AssertEqual(300, evidence.MemberCount, "public API baseline member count");
    Assert(evidence.BaselinePath.EndsWith("-public-api.txt", StringComparison.OrdinalIgnoreCase), "public API baseline should use a text artifact");
}

static void SigtranCommercialReleaseExecutionReadinessReportsRemainingBlockers()
{
    SigtranCommercialReleaseExecutionReadinessReport report = SigtranCommercialReleaseExecutionReadiness.CreateCurrent();

    Assert(report.CommercialReleaseReady == false, report.Describe());
    Assert(report.PassedCount > 0, "commercial release execution readiness should include passed evidence items");
    Assert(report.BlockedCount > 0, "commercial release execution readiness should include blocked evidence items");
    Assert(report.Items.Any(static item => item.Name == "external-peer-interop" && !item.Passed), "commercial release execution readiness should block external peer interop");
    Assert(report.Items.Any(static item => item.Name == "package-signing" && !item.Passed), "commercial release execution readiness should block incomplete signing verification");
    Assert(report.Items.Any(static item => item.Name == "performance" && !item.Passed), "commercial release execution readiness should block smoke-only performance evidence");
}

static void SigtranStatusCapabilitiesUseDomainDocumentationLabels()
{
    IReadOnlyList<string>[] statusCapabilities =
    [
        SigtranInteroperabilityToolingStatus.GetCompletedCapabilities(),
        SigtranCommercializationStatus.GetCompletedCapabilities(),
        SigtranReleaseAutomationStatus.GetCompletedCapabilities(),
        SigtranDeveloperExperienceStatus.GetCompletedCapabilities(),
        SigtranOperationsStatus.GetCompletedCapabilities(),
        SigtranComplianceStatus.GetCompletedCapabilities(),
        SigtranPerformanceStatus.GetCompletedCapabilities(),
        SigtranApiLifecycleStatus.GetCompletedCapabilities(),
        SigtranConfigurationStatus.GetCompletedCapabilities(),
        SigtranNativeSctpImplementationStatus.GetCompletedCapabilities(),
        SigtranNativeSctpLabVerificationStatus.GetCompletedCapabilities(),
        SigtranExternalPeerInteropStatus.GetCompletedCapabilities(),
        SigtranProtocolInteropStatus.GetCompletedCapabilities(),
        SigtranCommercialEvidenceStatus.GetCompletedCapabilities(),
        SigtranSupplyChainStatus.GetCompletedCapabilities(),
        SigtranReleaseWorkflowStatus.GetCompletedCapabilities(),
        SigtranPackagePublicationStatus.GetCompletedCapabilities()
    ];

    foreach (IReadOnlyList<string> capabilities in statusCapabilities)
    {
        Assert(capabilities.Contains("documentation"), "status capabilities should include documentation");
        Assert(!capabilities.Any(capability => capability.Contains("phase", StringComparison.OrdinalIgnoreCase)), "status capabilities should not include phase labels");
    }
}

static SigtranCommercialEvidenceManifest CreateCompleteCommercialEvidenceManifest()
{
    SigtranCommercialEvidenceManifest manifest = new();
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.NativeSctp, SigtranCommercialEvidenceArtifactKind.PacketCapture, "artifacts/commercial/native-sctp.pcapng", "SHA256-001"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.NativeSctp, SigtranCommercialEvidenceArtifactKind.SdkTrace, "artifacts/commercial/native-sctp-sdk.log", "SHA256-002"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.NativeSctp, SigtranCommercialEvidenceArtifactKind.PeerLog, "artifacts/commercial/native-sctp-peer.log", "SHA256-003"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.NativeSctp, SigtranCommercialEvidenceArtifactKind.ComparisonReport, "artifacts/commercial/native-sctp-comparison.md", "SHA256-004"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ExternalPeerInterop, SigtranCommercialEvidenceArtifactKind.PacketCapture, "artifacts/commercial/external-peer.pcapng", "SHA256-005"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ExternalPeerInterop, SigtranCommercialEvidenceArtifactKind.SdkTrace, "artifacts/commercial/external-peer-sdk.log", "SHA256-006"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ExternalPeerInterop, SigtranCommercialEvidenceArtifactKind.PeerConfiguration, "artifacts/commercial/external-peer.conf", "SHA256-007"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ExternalPeerInterop, SigtranCommercialEvidenceArtifactKind.PeerLog, "artifacts/commercial/external-peer.log", "SHA256-008"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ExternalPeerInterop, SigtranCommercialEvidenceArtifactKind.ComparisonReport, "artifacts/commercial/external-peer-comparison.md", "SHA256-009"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ProtocolInterop, SigtranCommercialEvidenceArtifactKind.ReferenceVector, "artifacts/commercial/protocol-reference.ber", "SHA256-010"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ProtocolInterop, SigtranCommercialEvidenceArtifactKind.SdkVector, "artifacts/commercial/protocol-sdk.ber", "SHA256-011"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ProtocolInterop, SigtranCommercialEvidenceArtifactKind.ComparisonReport, "artifacts/commercial/protocol-comparison.md", "SHA256-012"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ReleaseProvenance, SigtranCommercialEvidenceArtifactKind.ReleaseProvenance, "artifacts/commercial/provenance.json", "SHA256-013"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.ReleaseProvenance, SigtranCommercialEvidenceArtifactKind.PackageManifest, "artifacts/commercial/package-manifest.json", "SHA256-014"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.PackageArtifacts, SigtranCommercialEvidenceArtifactKind.Package, "artifacts/commercial/Sigtran.NET.1.0.0.nupkg", "SHA256-015"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.PackageArtifacts, SigtranCommercialEvidenceArtifactKind.SymbolPackage, "artifacts/commercial/Sigtran.NET.1.0.0.snupkg", "SHA256-016"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.PackageArtifacts, SigtranCommercialEvidenceArtifactKind.Sbom, "artifacts/commercial/sbom.spdx.json", "SHA256-017"));
    manifest.Add(new SigtranCommercialEvidenceArtifact(SigtranCommercialEvidenceArea.PackageArtifacts, SigtranCommercialEvidenceArtifactKind.Signature, "artifacts/commercial/package.sig", "SHA256-018"));
    return manifest;
}

static SigtranSupplyChainArtifactManifest CreateCompleteSupplyChainManifest()
{
    SigtranSupplyChainArtifactManifest manifest = new();
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.Sbom, "artifacts/supply-chain/Sigtran.NET.spdx.json", "SHA256-101"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.Signature, "artifacts/supply-chain/Sigtran.NET.1.0.0.nupkg.sig", "SHA256-102"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.TimestampReceipt, "artifacts/supply-chain/timestamp.tsr", "SHA256-103"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.ProvenanceAttestation, "artifacts/supply-chain/provenance.json", "SHA256-104"));
    manifest.Add(new SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind.VerificationReport, "artifacts/supply-chain/verification.md", "SHA256-105"));
    return manifest;
}

static void NativeSctpPlatformProbeReportsSocketCreationCapability()
{
    AssertEqual(132, NativeSctpPlatform.IpProtocolSctp, "native SCTP protocol number");
    AssertEqual(SocketType.Seqpacket, NativeSctpPlatform.SctpSocketType, "native SCTP socket type");

    NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();

    if (OperatingSystem.IsLinux())
    {
        Assert(
            capability.Status is NativeSctpPlatformStatus.SocketCreationSupported or NativeSctpPlatformStatus.SocketCreationUnavailable,
            capability.Describe());
    }
    else
    {
        AssertEqual(NativeSctpPlatformStatus.UnsupportedOperatingSystem, capability.Status, "native SCTP unsupported OS status");
    }

    Assert(capability.Describe().Contains("nativeSctp", StringComparison.Ordinal), capability.Describe());
}

static void NativeSctpSocketFactoryCreatesOrReportsUnsupportedPlatform()
{
    NativeSctpSocketFactory factory = new();
    NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();

    if (capability.CanCreateSocket)
    {
        using Socket socket = factory.CreateSocket();
        AssertEqual(SocketType.Seqpacket, socket.SocketType, "native SCTP factory socket type");
        AssertEqual(NativeSctpPlatform.SctpProtocolType, socket.ProtocolType, "native SCTP factory protocol type");
    }
    else
    {
        NativeSctpUnavailableException exception = AssertThrows<NativeSctpUnavailableException>(() => factory.CreateSocket());
        AssertEqual(capability.Status, exception.Capability.Status, "native SCTP unavailable status");
    }
}

static void NativeSctpConnectionPlannerResolvesEndpoints()
{
    SctpConnectionOptions options = new(
        new SctpEndpoint("127.0.0.1", 2905),
        new SctpEndpoint("127.0.0.1", 2906));
    NativeSctpConnectionPlanner planner = new();

    NativeSctpConnectionPlan plan = planner.BuildAsync(options).GetAwaiter().GetResult();

    AssertEqual("127.0.0.1", plan.RemoteEndpoint.Address.ToString(), "native SCTP remote address");
    AssertEqual(2905, plan.RemoteEndpoint.Port, "native SCTP remote port");
    AssertEqual(2906, plan.LocalEndpoint!.Port, "native SCTP local port");
    Assert(plan.Describe().Contains("remote=127.0.0.1:2905", StringComparison.Ordinal), plan.Describe());
}

static void NativeSctpSocketAdapterReportsLifecycleHealth()
{
    NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();
    if (!capability.CanCreateSocket)
    {
        Assert(!capability.CanCreateSocket, capability.Describe());
        return;
    }

    using Socket socket = new NativeSctpSocketFactory().CreateSocket();
    SctpConnectionOptions options = new(new SctpEndpoint("127.0.0.1", 2905), outboundStreams: 2, inboundStreams: 3);
    using NativeSctpSocketAdapter adapter = new(socket, options);

    AssertEqual(SctpAssociationState.Closed, adapter.AssociationState, "native adapter initial state");
    adapter.MarkEstablished();
    SctpTransportHealth health = adapter.GetHealthSnapshot();

    Assert(health.IsEstablished, "native adapter health should be established");
    AssertEqual((ushort)2, health.OutboundStreams, "native adapter outbound streams");
    AssertEqual((ushort)3, health.InboundStreams, "native adapter inbound streams");
    AssertEqual((uint)SctpPayloadProtocolIdentifiers.M3ua, health.DefaultPayloadProtocolIdentifier, "native adapter PPID");
}

static void NativeSctpConnectorReportsUnsupportedPlatformSafely()
{
    NativeSctpConnector connector = new();
    NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();
    SctpConnectionOptions options = new(
        new SctpEndpoint("127.0.0.1", 2905),
        connectTimeout: TimeSpan.FromMilliseconds(10));

    if (!capability.CanCreateSocket)
    {
        NativeSctpUnavailableException exception = AssertThrows<NativeSctpUnavailableException>(() =>
            connector.ConnectAsync(options).GetAwaiter().GetResult());
        AssertEqual(capability.Status, exception.Capability.Status, "native SCTP connector unsupported status");
    }
    else
    {
        Assert(capability.CanCreateSocket, capability.Describe());
    }
}

static void NativeSctpListenerValidatesOptionsAndUnsupportedPlatform()
{
    AssertThrows<ArgumentOutOfRangeException>(() => new NativeSctpListenerOptions(new SctpEndpoint("127.0.0.1", 2905), backlog: 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new NativeSctpListenerOptions(new SctpEndpoint("127.0.0.1", 2905), outboundStreams: 0));

    NativeSctpListenerOptions options = new(new SctpEndpoint("127.0.0.1", 2905), backlog: 1);
    AssertEqual((uint)SctpPayloadProtocolIdentifiers.M3ua, options.DefaultPayloadProtocolIdentifier, "native listener default PPID");

    NativeSctpPlatformCapability capability = NativeSctpPlatform.Probe();
    if (!capability.CanCreateSocket)
    {
        using NativeSctpListener listener = new();
        NativeSctpUnavailableException exception = AssertThrows<NativeSctpUnavailableException>(() =>
            listener.StartAsync(options).GetAwaiter().GetResult());
        AssertEqual(capability.Status, exception.Capability.Status, "native listener unsupported status");
    }
}

static void NativeSctpLabProfileIsOptIn()
{
    string? previous = Environment.GetEnvironmentVariable(NativeSctpLabProfile.EnableEnvironmentVariable);
    try
    {
        Environment.SetEnvironmentVariable(NativeSctpLabProfile.EnableEnvironmentVariable, null);
        NativeSctpLabProfile disabled = NativeSctpLab.CreateFromEnvironment();
        Assert(!disabled.Enabled, "native SCTP lab should be disabled by default");
        AssertEqual("127.0.0.1", disabled.LoopbackEndpoint.Host, "native SCTP lab loopback host");

        Environment.SetEnvironmentVariable(NativeSctpLabProfile.EnableEnvironmentVariable, "1");
        NativeSctpLabProfile enabled = NativeSctpLab.CreateFromEnvironment();
        Assert(enabled.Enabled, "native SCTP lab should enable from environment");
        Assert(enabled.Describe().Contains("enabled=True", StringComparison.Ordinal), enabled.Describe());
    }
    finally
    {
        Environment.SetEnvironmentVariable(NativeSctpLabProfile.EnableEnvironmentVariable, previous);
    }
}

static void NativeSctpReadinessReportsFoundationAndVerificationGates()
{
    NativeSctpReadinessReport report = NativeSctpReadiness.GetReport();

    Assert(report.FoundationReady, "native SCTP foundation should be ready");
    Assert(!report.IsProductionReady, "native SCTP production should wait for Linux verification");
    AssertEqual(NativeSctpReadiness.RequiredFoundationCapabilityCount, report.FoundationCapabilityCount, "native SCTP foundation count");
    Assert(report.Describe().Contains("linuxVerified=False", StringComparison.Ordinal), report.Describe());
}

static void SigtranNativeSctpImplementationStatusSummarizesNativeSctpFoundation()
{
    IReadOnlyList<string> capabilities = SigtranNativeSctpImplementationStatus.GetCompletedCapabilities();

    AssertEqual("Native SCTP Production Transport", SigtranNativeSctpImplementationStatus.StatusLabel, "native SCTP implementation label");
    AssertEqual(10, SigtranNativeSctpImplementationStatus.CompletedUnitCount, "native SCTP implementation completed unit count");
    AssertEqual(10, capabilities.Count, "native SCTP implementation capability count");
    Assert(capabilities.Contains("native-sctp-listener"), "native SCTP implementation status should include listener");
    Assert(SigtranNativeSctpImplementationStatus.Describe().Contains("foundationReady=True", StringComparison.Ordinal), SigtranNativeSctpImplementationStatus.Describe());
    Assert(SigtranNativeSctpImplementationStatus.Describe().Contains("productionReady=False", StringComparison.Ordinal), SigtranNativeSctpImplementationStatus.Describe());
}

static void TcapBerElementEncodesShortAndLongLengths()
{
    Span<byte> shortBuffer = stackalloc byte[8];
    TcapBerTag invokeTag = new(TcapBerTagClass.ContextSpecific, constructed: true, number: 1);
    Assert(TcapBer.TryWriteElement(shortBuffer, invokeTag, [0x01, 0x02], out int shortWritten, out string? error), error ?? "BER short write failed");
    AssertEqual(4, shortWritten, "BER short written length");
    AssertSequence([0xA1, 0x02, 0x01, 0x02], shortBuffer.Slice(0, shortWritten), "BER short element bytes");
    Assert(TcapBer.TryReadElement(shortBuffer.Slice(0, shortWritten), out TcapBerElement shortElement, out error), error ?? "BER short read failed");
    AssertEqual(TcapBerTagClass.ContextSpecific, shortElement.Tag.TagClass, "BER short tag class");
    Assert(shortElement.Tag.Constructed, "BER short tag constructed");
    AssertEqual((byte)1, shortElement.Tag.Number, "BER short tag number");
    AssertSequence([0x01, 0x02], shortElement.Value.Span, "BER short value");

    byte[] longValue = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();
    byte[] longBuffer = new byte[140];
    Assert(TcapBer.TryWriteElement(longBuffer, new TcapBerTag(TcapBerTagClass.Universal, constructed: false, number: 4), longValue, out int longWritten, out error), error ?? "BER long write failed");
    AssertEqual((byte)0x04, longBuffer[0], "BER octet string tag");
    AssertEqual((byte)0x81, longBuffer[1], "BER long length marker");
    AssertEqual((byte)130, longBuffer[2], "BER long length value");
    Assert(TcapBer.TryReadElement(longBuffer.AsSpan(0, longWritten), out TcapBerElement longElement, out error), error ?? "BER long read failed");
    AssertEqual(130, longElement.Value.Length, "BER long value length");
}

static void TcapTransactionIdentifiersUseBerContextTags()
{
    TcapTransactionId transactionId = TcapTransactionId.FromUInt32(0x010203);
    AssertEqual(3, transactionId.Length, "TCAP transaction id length");
    AssertEqual("010203", transactionId.ToString(), "TCAP transaction id hex");

    Span<byte> buffer = stackalloc byte[8];
    Assert(
        TcapBer.TryWriteElement(buffer, TcapTransactionTags.TransactionId(originating: true), transactionId.ToArray(), out int written, out string? error),
        error ?? "TCAP transaction id write failed");
    AssertSequence([0x88, 0x03, 0x01, 0x02, 0x03], buffer.Slice(0, written), "TCAP originating transaction id TLV");
    Assert(TcapBer.TryReadElement(buffer.Slice(0, written), out TcapBerElement element, out error), error ?? "TCAP transaction id read failed");
    AssertEqual(TcapBerTagClass.ContextSpecific, element.Tag.TagClass, "TCAP transaction id tag class");
    AssertEqual((byte)TcapTransactionTags.OriginatingTransactionId, element.Tag.Number, "TCAP transaction id tag number");

    TcapBerTag beginTag = TcapTransactionTags.Package(TcapPackageType.Begin);
    AssertEqual((byte)0x62, beginTag.Encode(), "TCAP Begin package tag");
    AssertThrows<ArgumentException>(() => new TcapTransactionId(ReadOnlySpan<byte>.Empty));
}

static void TcapBerInvokeComponentRoundTrips()
{
    TcapBerInvokeComponent invoke = new(
        invokeId: 7,
        TcapOperationCode.MoForwardShortMessage,
        new byte[] { 0xAA, 0xBB },
        linkedInvokeId: 3);

    byte[] encoded = invoke.Encode();
    AssertSequence([0xA1, 0x0D, 0x02, 0x01, 0x07, 0x02, 0x01, 0x03, 0x02, 0x01, 0x01, 0x04, 0x02, 0xAA, 0xBB], encoded, "TCAP BER Invoke bytes");
    Assert(TcapBerInvokeComponent.TryDecode(encoded, out TcapBerInvokeComponent? decoded, out string? error), error ?? "TCAP BER Invoke decode failed");
    AssertEqual((byte)7, decoded!.InvokeId, "TCAP decoded Invoke ID");
    AssertEqual((byte)3, decoded.LinkedInvokeId, "TCAP decoded linked Invoke ID");
    AssertEqual(TcapOperationCode.MoForwardShortMessage, decoded.OperationCode, "TCAP decoded operation code");
    AssertSequence([0xAA, 0xBB], decoded.Parameters.Span, "TCAP decoded Invoke parameters");
}

static void TcapBerOutcomeComponentsRoundTrip()
{
    TcapBerReturnResultComponent result = new(7, TcapOperationCode.MoForwardShortMessage, new byte[] { 0x10, 0x20 });
    byte[] resultBytes = result.Encode();
    AssertSequence([0xA2, 0x0A, 0x02, 0x01, 0x07, 0x02, 0x01, 0x01, 0x04, 0x02, 0x10, 0x20], resultBytes, "TCAP ReturnResult bytes");
    Assert(TcapBerReturnResultComponent.TryDecode(resultBytes, out TcapBerReturnResultComponent? decodedResult, out string? error), error ?? "TCAP ReturnResult decode failed");
    AssertEqual((byte)7, decodedResult!.InvokeId, "TCAP ReturnResult invoke id");
    AssertEqual(TcapOperationCode.MoForwardShortMessage, decodedResult.OperationCode, "TCAP ReturnResult operation");
    AssertSequence([0x10, 0x20], decodedResult.Parameters.Span, "TCAP ReturnResult parameters");

    TcapBerReturnErrorComponent returnError = new(7, TcapReturnErrorCode.SystemFailure, new byte[] { 0x01 });
    byte[] errorBytes = returnError.Encode();
    Assert(TcapBerReturnErrorComponent.TryDecode(errorBytes, out TcapBerReturnErrorComponent? decodedError, out error), error ?? "TCAP ReturnError decode failed");
    AssertEqual(TcapReturnErrorCode.SystemFailure, decodedError!.ErrorCode, "TCAP ReturnError code");
    AssertSequence([0x01], decodedError.Parameters.Span, "TCAP ReturnError parameters");

    TcapBerRejectComponent reject = new(7, TcapRejectProblemCode.DuplicateInvokeId);
    byte[] rejectBytes = reject.Encode();
    AssertSequence([0xA4, 0x06, 0x02, 0x01, 0x07, 0x02, 0x01, 0x02], rejectBytes, "TCAP Reject bytes");
    Assert(TcapBerRejectComponent.TryDecode(rejectBytes, out TcapBerRejectComponent? decodedReject, out error), error ?? "TCAP Reject decode failed");
    AssertEqual(TcapRejectProblemCode.DuplicateInvokeId, decodedReject!.ProblemCode, "TCAP Reject problem code");
}

static void TcapTransactionMessageWrapsComponentPortion()
{
    TcapBerInvokeComponent invoke = new(1, TcapOperationCode.MtForwardShortMessage, new byte[] { 0xAA });
    TcapTransactionMessage begin = new(
        TcapPackageType.Begin,
        originatingTransactionId: TcapTransactionId.FromUInt32(0x0102),
        componentPortion: invoke.Encode());

    byte[] encoded = begin.Encode();
    AssertEqual((byte)0x62, encoded[0], "TCAP Begin package tag");
    Assert(TcapTransactionMessage.TryDecode(encoded, out TcapTransactionMessage? decoded, out string? error), error ?? "TCAP transaction decode failed");
    AssertEqual(TcapPackageType.Begin, decoded!.PackageType, "TCAP decoded package type");
    AssertEqual("0102", decoded.OriginatingTransactionId?.ToString(), "TCAP decoded originating id");
    Assert(!decoded.ComponentPortion.IsEmpty, "TCAP decoded component portion should be present");
    Assert(TcapBerInvokeComponent.TryDecode(decoded.ComponentPortion.Span, out TcapBerInvokeComponent? decodedInvoke, out error), error ?? "TCAP decoded component portion failed");
    AssertEqual(TcapOperationCode.MtForwardShortMessage, decodedInvoke!.OperationCode, "TCAP decoded nested invoke operation");
}

static void TcapDialoguePortionCarriesApplicationContext()
{
    TcapObjectIdentifier oid = new(0, 0, 17, 773, 1, 1, 1);
    TcapDialoguePortion dialogue = new(oid, new byte[] { 0x55, 0x66 });
    byte[] encoded = dialogue.Encode();
    AssertEqual((byte)0x06, encoded[0], "TCAP dialogue OID tag");
    Assert(TcapDialoguePortion.TryDecode(encoded, out TcapDialoguePortion? decoded, out string? error), error ?? "TCAP dialogue decode failed");
    AssertEqual("0.0.17.773.1.1.1", decoded!.ApplicationContext.ToString(), "TCAP decoded application context");
    AssertSequence([0x55, 0x66], decoded.UserInformation.Span, "TCAP decoded dialogue user information");

    TcapTransactionMessage begin = new(
        TcapPackageType.Begin,
        originatingTransactionId: TcapTransactionId.FromUInt32(1),
        dialoguePortion: encoded);
    Assert(TcapTransactionMessage.TryDecode(begin.Encode(), out TcapTransactionMessage? decodedBegin, out error), error ?? "TCAP transaction with dialogue decode failed");
    Assert(TcapDialoguePortion.TryDecode(decodedBegin!.DialoguePortion.Span, out TcapDialoguePortion? nested, out error), error ?? "TCAP nested dialogue decode failed");
    AssertEqual(oid.ToString(), nested!.ApplicationContext.ToString(), "TCAP nested dialogue OID");
}

static void TcapDialogueControllerTracksStateAndInvokeTimeouts()
{
    DateTimeOffset sentAt = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
    TcapDialogueController dialogue = new(
        dialogueId: 100,
        new TcapInvokeTimeoutPolicy(TimeSpan.FromSeconds(5), maxPendingInvokes: 1));

    AssertEqual(TcapDialoguePhase.Idle, dialogue.Phase, "TCAP initial dialogue phase");
    dialogue.Begin();
    AssertEqual(TcapDialoguePhase.Open, dialogue.Phase, "TCAP begin dialogue phase");
    dialogue.RegisterInvoke(1, sentAt);
    AssertEqual(1, dialogue.PendingInvokeCount, "TCAP pending invoke count");
    Assert(!dialogue.IsInvokeTimedOut(1, sentAt.AddSeconds(4)), "TCAP invoke should not be timed out yet");
    Assert(dialogue.IsInvokeTimedOut(1, sentAt.AddSeconds(5)), "TCAP invoke should be timed out");
    AssertThrows<InvalidOperationException>(() => dialogue.RegisterInvoke(1, sentAt));
    Assert(dialogue.CompleteInvoke(1), "TCAP pending invoke should complete");
    dialogue.Continue();
    AssertEqual(TcapDialoguePhase.Continuing, dialogue.Phase, "TCAP continue dialogue phase");
    dialogue.End();
    AssertEqual(TcapDialoguePhase.Ended, dialogue.Phase, "TCAP ended dialogue phase");
}

static void TcapAllocatorsIssueTransactionAndInvokeIdentifiers()
{
    TcapTransactionIdAllocator transactionIds = new(firstValue: 0xFE, maxValue: 0xFF);
    AssertEqual("FE", transactionIds.Allocate().ToString(), "TCAP transaction allocator first id");
    AssertEqual("FF", transactionIds.Allocate().ToString(), "TCAP transaction allocator second id");
    AssertEqual("01", transactionIds.Allocate().ToString(), "TCAP transaction allocator wraps");

    TcapInvokeRegistry invokes = new();
    byte first = invokes.Allocate();
    AssertEqual((byte)1, first, "TCAP invoke allocator first id");
    invokes.Register(9);
    AssertEqual(2, invokes.Count, "TCAP invoke registry count");
    AssertThrows<InvalidOperationException>(() => invokes.Register(9));
    Assert(invokes.Complete(first), "TCAP invoke registry completes allocated id");
    AssertEqual(1, invokes.Count, "TCAP invoke registry count after complete");
}

static void TcapSessionBuilderCreatesBeginAndEndMessages()
{
    TcapSessionBuilder builder = new(new TcapTransactionIdAllocator(firstValue: 3), new TcapInvokeRegistry());
    TcapBuiltInvoke built = builder.BeginInvoke(new TcapObjectIdentifier(0, 0, 17, 773, 1, 1, 1), TcapOperationCode.MoForwardShortMessage, new byte[] { 0xCA });
    AssertEqual("03", built.OriginatingTransactionId.ToString(), "TCAP built transaction id");
    AssertEqual((byte)1, built.InvokeId, "TCAP built invoke id");
    Assert(TcapTransactionMessage.TryDecode(built.EncodedMessage, out TcapTransactionMessage? decodedBegin, out string? error), error ?? "TCAP built Begin decode failed");
    AssertEqual(TcapPackageType.Begin, decodedBegin!.PackageType, "TCAP built Begin package");

    byte[] end = builder.EndResult(built.OriginatingTransactionId, built.InvokeId, TcapOperationCode.MoForwardShortMessage, new byte[] { 0x01 });
    Assert(TcapTransactionMessage.TryDecode(end, out TcapTransactionMessage? decodedEnd, out error), error ?? "TCAP built End decode failed");
    AssertEqual(TcapPackageType.End, decodedEnd!.PackageType, "TCAP built End package");
    AssertEqual("03", decodedEnd.DestinationTransactionId?.ToString(), "TCAP built End destination id");
}

static void TcapReadinessReportsFoundationStatus()
{
    AssertEqual("TCAP BER foundation", TcapReadiness.ReleaseLabel, "TCAP readiness label");
    AssertEqual(7, TcapReadiness.RequiredFoundationCapabilityCount, "TCAP readiness capability count");
    AssertEqual(7, TcapReadiness.GetFoundationCapabilities().Count, "TCAP readiness capability name count");
    Assert(
        TcapReadiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        TcapReadiness.ProductionGateDescription);

    TcapReadinessReport report = TcapReadiness.GetReport();
    Assert(report.FoundationReady, "TCAP foundation should be ready");
    Assert(!report.IsProductionReady, "TCAP should not claim production readiness without interop vectors");
    AssertEqual(7, report.FoundationCapabilityCount, "TCAP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=7/7", StringComparison.Ordinal), report.Describe());
}

static void MapSmsOperationCatalogAndParameterSetEncodeBer()
{
    Assert(MapSmsOperationCatalog.TryGet(MapSmsOperationCode.MoForwardShortMessage, out MapSmsOperationMetadata metadata), "MAP MO operation metadata should exist");
    AssertEqual("mo-ForwardSM", metadata.Name, "MAP MO operation name");
    AssertEqual(5, MapSmsOperationCatalog.GetSupportedOperations().Count, "MAP supported SMS operation count");

    MapSmsParameterSet parameters = new();
    parameters.Add(0, [0x01, 0x02]);
    parameters.Add(1, [0xAA]);
    byte[] encoded = parameters.Encode();
    AssertSequence([0x80, 0x02, 0x01, 0x02, 0x81, 0x01, 0xAA], encoded, "MAP SMS parameter set BER");

    Assert(MapSmsParameterSet.TryDecode(encoded, out MapSmsParameterSet? decoded, out string? error), error ?? "MAP parameter decode failed");
    AssertEqual(2, decoded!.Snapshot().Count, "MAP decoded parameter count");
    AssertSequence([0xAA], decoded.Snapshot()[1].Value.Span, "MAP decoded second parameter");
}

static void MapSmsAddressPrimitivesEncodeTbcdDigits()
{
    byte[] tbcd = MapSmsAddress.EncodeTbcd("44123456789");
    AssertSequence([0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], tbcd, "MAP TBCD bytes");
    AssertEqual("44123456789", MapSmsAddress.DecodeTbcd(tbcd, oddDigitCount: true), "MAP decoded TBCD digits");

    MapSmsAddress msisdn = new(MapSmsAddressKind.Msisdn, "+44123456789");
    byte[] encoded = msisdn.Encode();
    AssertSequence([0x01, 0x04, 0x01, 0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], encoded, "MAP MSISDN address bytes");
    MapSmsAddress decoded = MapSmsAddress.Decode(encoded, oddDigitCount: true);
    AssertEqual(MapSmsAddressKind.Msisdn, decoded.Kind, "MAP decoded address kind");
    AssertEqual("44123456789", decoded.Digits, "MAP decoded address digits");
    AssertThrows<ArgumentException>(() => new MapSmsAddress(MapSmsAddressKind.Imsi, "12A"));
}

static void MapMoForwardSmModelEncodesRequiredParameters()
{
    MapMoForwardShortMessage mo = new(
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new byte[] { 0x11, 0x22 });

    byte[] encoded = mo.Encode();
    Assert(encoded[0] == 0x80 && encoded.Contains((byte)0x82), "MAP MO encoded parameter tags should be present");
    Assert(MapMoForwardShortMessage.TryDecode(encoded, out MapMoForwardShortMessage? decoded, out string? error), error ?? "MAP MO decode failed");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded!.SmRpDa.Kind, "MAP MO decoded DA kind");
    AssertEqual("989121234567", decoded.SmRpOa.Digits, "MAP MO decoded OA digits");
    AssertSequence([0x11, 0x22], decoded.SmRpUi.Span, "MAP MO decoded user information");

    byte[] helper = MapSmsOperations.CreateMoForwardSm(decoded.SmRpDa, decoded.SmRpOa, decoded.SmRpUi.Span);
    AssertSequence(encoded, helper, "MAP MO helper encoding");
}

static void MapMtForwardSmModelEncodesRequiredParameters()
{
    MapMtForwardShortMessage mt = new(
        new MapSmsAddress(MapSmsAddressKind.Imsi, "432109876543210"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new byte[] { 0x21, 0x43 });

    byte[] encoded = mt.Encode();
    Assert(MapMtForwardShortMessage.TryDecode(encoded, out MapMtForwardShortMessage? decoded, out string? error), error ?? "MAP MT decode failed");
    AssertEqual(MapSmsAddressKind.Imsi, decoded!.SmRpDa.Kind, "MAP MT decoded DA kind");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded.SmRpOa.Kind, "MAP MT decoded OA kind");
    AssertSequence([0x21, 0x43], decoded.SmRpUi.Span, "MAP MT decoded user information");

    byte[] helper = MapSmsOperations.CreateMtForwardSm(decoded.SmRpDa, decoded.SmRpOa, decoded.SmRpUi.Span);
    AssertSequence(encoded, helper, "MAP MT helper encoding");
}

static void MapSendRoutingInfoForSmModelEncodesRoutingParameters()
{
    MapSendRoutingInfoForShortMessage sri = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        gprsSupportIndicator: true);

    byte[] encoded = sri.Encode();
    Assert(MapSendRoutingInfoForShortMessage.TryDecode(encoded, out MapSendRoutingInfoForShortMessage? decoded, out string? error), error ?? "MAP SRI-SM decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP SRI decoded MSISDN");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded.ServiceCentreAddress.Kind, "MAP SRI decoded SC address kind");
    Assert(decoded.GprsSupportIndicator, "MAP SRI decoded GPRS indicator");
}

static void MapReportSmDeliveryStatusModelEncodesDeliveryStatus()
{
    MapReportShortMessageDeliveryStatus report = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        MapSmsDeliveryStatus.MemoryCapacityExceeded);

    byte[] encoded = report.Encode();
    Assert(MapReportShortMessageDeliveryStatus.TryDecode(encoded, out MapReportShortMessageDeliveryStatus? decoded, out string? error), error ?? "MAP ReportSM decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP ReportSM decoded MSISDN");
    AssertEqual(MapSmsDeliveryStatus.MemoryCapacityExceeded, decoded.DeliveryStatus, "MAP ReportSM decoded status");
}

static void MapAlertServiceCentreModelEncodesAlertParameters()
{
    MapAlertServiceCentre alert = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"));

    byte[] encoded = alert.Encode();
    Assert(MapAlertServiceCentre.TryDecode(encoded, out MapAlertServiceCentre? decoded, out string? error), error ?? "MAP AlertSC decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP AlertSC decoded MSISDN");
    AssertEqual("441234", decoded.ServiceCentreAddress.Digits, "MAP AlertSC decoded SC address");
}

static void MapSmsErrorMapperAndExtensionContainerEncodeValues()
{
    AssertEqual(
        MapSmsDeliveryStatus.AbsentSubscriber,
        MapSmsErrorMapper.ToDeliveryStatus(MapSmsErrorCode.AbsentSubscriberForShortMessage),
        "MAP absent subscriber error mapping");

    MapSmsExtensionContainer extensions = new();
    extensions.Add(5, [0xCA, 0xFE]);
    byte[] encoded = extensions.Encode();
    AssertSequence([0x85, 0x02, 0xCA, 0xFE], encoded, "MAP extension container bytes");
    Assert(MapSmsExtensionContainer.TryDecode(encoded, out MapSmsExtensionContainer? decoded, out string? error), error ?? "MAP extension decode failed");
    AssertEqual(1, decoded!.Snapshot().Count, "MAP extension count");
}

static void MapSmsTcapClientBuildsBeginInvokeTransactions()
{
    MapSmsTcapClient client = new(new TcapSessionBuilder(new TcapTransactionIdAllocator(firstValue: 7), new TcapInvokeRegistry()));
    MapMoForwardShortMessage mo = new(
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new byte[] { 0x11 });

    TcapBuiltInvoke built = client.BeginMoForwardShortMessage(mo);
    AssertEqual("07", built.OriginatingTransactionId.ToString(), "MAP TCAP built transaction id");
    Assert(TcapTransactionMessage.TryDecode(built.EncodedMessage, out TcapTransactionMessage? transaction, out string? error), error ?? "MAP TCAP transaction decode failed");
    Assert(TcapBerInvokeComponent.TryDecode(transaction!.ComponentPortion.Span, out TcapBerInvokeComponent? invoke, out error), error ?? "MAP TCAP invoke decode failed");
    AssertEqual((TcapOperationCode)MapSmsOperationCode.MoForwardShortMessage, invoke!.OperationCode, "MAP TCAP operation code");
    Assert(MapMoForwardShortMessage.TryDecode(invoke.Parameters.Span, out MapMoForwardShortMessage? decodedMo, out error), error ?? "MAP TCAP MO params decode failed");
    AssertEqual("989121234567", decodedMo!.SmRpOa.Digits, "MAP TCAP decoded MO originator");
}

static void MapSmsReadinessReportsFoundationStatus()
{
    AssertEqual("MAP SMS profile foundation", MapSmsReadiness.ReleaseLabel, "MAP readiness label");
    AssertEqual(8, MapSmsReadiness.RequiredFoundationCapabilityCount, "MAP readiness capability count");
    Assert(
        MapSmsReadiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        MapSmsReadiness.ProductionGateDescription);

    MapSmsReadinessReport report = MapSmsReadiness.GetReport();
    Assert(report.FoundationReady, "MAP SMS foundation should be ready");
    Assert(!report.IsProductionReady, "MAP SMS should not claim production readiness without interop vectors");
    AssertEqual(8, report.FoundationCapabilityCount, "MAP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=8/8", StringComparison.Ordinal), report.Describe());
}

static void Mtp3RoutingLabelAndSioRoundTrip()
{
    Mtp3ServiceInformationOctet sio = new(Mtp3ServiceIndicator.Sccp, networkIndicator: 2, messagePriority: 1);
    byte encodedSio = sio.Encode();
    AssertEqual((byte)0x93, encodedSio, "MTP3 SIO encoding");
    Mtp3ServiceInformationOctet decodedSio = Mtp3ServiceInformationOctet.Decode(encodedSio);
    AssertEqual(Mtp3ServiceIndicator.Sccp, decodedSio.ServiceIndicator, "MTP3 decoded SI");
    AssertEqual((byte)2, decodedSio.NetworkIndicator, "MTP3 decoded NI");
    AssertEqual((byte)1, decodedSio.MessagePriority, "MTP3 decoded MP");

    Mtp3RoutingLabel label = new(destinationPointCode: 0x1234, originatingPointCode: 0x2345, signallingLinkSelection: 0x0A);
    Span<byte> bytes = stackalloc byte[4];
    label.Encode(bytes);
    AssertSequence([0x34, 0x52, 0xD1, 0xA8], bytes, "MTP3 routing label bytes");
    Mtp3RoutingLabel decoded = Mtp3RoutingLabel.Decode(bytes);
    AssertEqual(0x1234U, decoded.DestinationPointCode, "MTP3 decoded DPC");
    AssertEqual(0x2345U, decoded.OriginatingPointCode, "MTP3 decoded OPC");
    AssertEqual((byte)0x0A, decoded.SignallingLinkSelection, "MTP3 decoded SLS");
}

static void SccpProtocolConstantsExposeConnectionlessClasses()
{
    AssertEqual((byte)0x09, (byte)SccpMessageType.Unitdata, "SCCP UDT message type");
    AssertEqual((byte)0x11, (byte)SccpMessageType.ExtendedUnitdata, "SCCP XUDT message type");
    AssertEqual((byte)0x13, (byte)SccpMessageType.LongUnitdata, "SCCP LUDT message type");

    SccpProtocolClass protocolClass = new(SccpConnectionlessClass.Class1, returnMessageOnError: true);
    AssertEqual((byte)0x81, protocolClass.Encode(), "SCCP protocol class encoding");
    SccpProtocolClass decoded = SccpProtocolClass.Decode(0x81);
    AssertEqual(SccpConnectionlessClass.Class1, decoded.ConnectionlessClass, "SCCP decoded protocol class");
    Assert(decoded.ReturnMessageOnError, "SCCP return-on-error flag");
}

static void SccpPartyAddressEncodesSsnAndGlobalTitle()
{
    SccpPartyAddress address = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        pointCode: 0x1234,
        globalTitle: new SccpGlobalTitle("44123456789", translationType: 0, numberingPlan: 1, natureOfAddress: 4));

    byte[] encoded = address.Encode();
    AssertEqual((byte)0x13, encoded[0], "SCCP party address indicator");
    AssertSequence([0x13, 0x34, 0x12, 0x06, 0x00, 0x11, 0x04, 0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], encoded, "SCCP party address bytes");

    Assert(SccpPartyAddress.TryDecode(encoded, out SccpPartyAddress? decoded, out string? error), error ?? "SCCP party address decode failed");
    AssertEqual(SccpRoutingIndicator.RouteOnGlobalTitle, decoded!.RoutingIndicator, "SCCP decoded routing indicator");
    AssertEqual(SubsystemNumber.MAP, decoded.SubsystemNumber, "SCCP decoded SSN");
    AssertEqual((ushort)0x1234, decoded.PointCode, "SCCP decoded point code");
    AssertEqual("44123456789", decoded.GlobalTitle?.Digits, "SCCP decoded GT digits");
}

static void SccpUdtCodecUsesVariableParameterPointers()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);

    SccpUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class0),
        called,
        calling,
        new byte[] { 0xCA, 0xFE });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.Unitdata, encoded[0], "SCCP UDT message type");
    AssertEqual((byte)3, encoded[2], "SCCP called pointer");
    AssertEqual((byte)14, encoded[3], "SCCP calling pointer");
    AssertEqual((byte)18, encoded[4], "SCCP data pointer");

    Assert(SccpUnitdataMessage.TryDecode(encoded, out SccpUnitdataMessage? decoded, out string? error), error ?? "SCCP UDT decode failed");
    AssertEqual(SccpConnectionlessClass.Class0, decoded!.ProtocolClass.ConnectionlessClass, "SCCP decoded UDT protocol class");
    AssertEqual("44123456789", decoded.CalledParty.GlobalTitle?.Digits, "SCCP decoded UDT called GT");
    AssertEqual(SubsystemNumber.MSC, decoded.CallingParty.SubsystemNumber, "SCCP decoded UDT calling SSN");
    AssertSequence([0xCA, 0xFE], decoded.UserData.Span, "SCCP decoded UDT data");
}

static void SccpXudtCodecPreservesHopCounter()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);

    SccpExtendedUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1, returnMessageOnError: true),
        hopCounter: 12,
        called,
        calling,
        new byte[] { 0x01, 0x02, 0x03 });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.ExtendedUnitdata, encoded[0], "SCCP XUDT message type");
    AssertEqual((byte)0x81, encoded[1], "SCCP XUDT protocol class");
    AssertEqual((byte)12, encoded[2], "SCCP XUDT hop counter");
    AssertEqual((byte)4, encoded[3], "SCCP XUDT called pointer");
    AssertEqual((byte)15, encoded[4], "SCCP XUDT calling pointer");
    AssertEqual((byte)19, encoded[5], "SCCP XUDT data pointer");
    AssertEqual((byte)0, encoded[6], "SCCP XUDT optional pointer");

    Assert(SccpExtendedUnitdataMessage.TryDecode(encoded, out SccpExtendedUnitdataMessage? decoded, out string? error), error ?? "SCCP XUDT decode failed");
    AssertEqual((byte)12, decoded!.HopCounter, "SCCP decoded XUDT hop counter");
    Assert(decoded.ProtocolClass.ReturnMessageOnError, "SCCP decoded XUDT return-on-error");
    AssertSequence([0x01, 0x02, 0x03], decoded.UserData.Span, "SCCP decoded XUDT data");
}

static void SccpSegmentationParameterRoundTrips()
{
    SccpSegmentationParameter segmentation = new(localReference: 0x00A1B2C3, remainingSegments: 3, firstSegment: true);
    Span<byte> encoded = stackalloc byte[SccpSegmentationParameter.EncodedLength];
    segmentation.Encode(encoded);
    AssertSequence([0x83, 0xA1, 0xB2, 0xC3], encoded, "SCCP segmentation bytes");

    SccpSegmentationParameter decoded = SccpSegmentationParameter.Decode(encoded);
    AssertEqual(0x00A1B2C3U, decoded.LocalReference, "SCCP decoded segmentation local reference");
    AssertEqual((byte)3, decoded.RemainingSegments, "SCCP decoded remaining segments");
    Assert(decoded.FirstSegment, "SCCP decoded first segment flag");
    Assert(decoded.Describe().Contains("remaining=3", StringComparison.Ordinal), decoded.Describe());
    AssertThrows<ArgumentOutOfRangeException>(() => new SccpSegmentationParameter(0x01000000, 0, false));
}

static void SccpXudtCarriesSegmentationOptionalParameter()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);
    SccpSegmentationParameter segmentation = new(0x00010203, remainingSegments: 1, firstSegment: false);

    SccpExtendedUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1),
        hopCounter: 10,
        called,
        calling,
        new byte[] { 0xAA, 0xBB, 0xCC },
        segmentation);

    byte[] encoded = message.Encode();
    AssertEqual((byte)22, encoded[6], "SCCP XUDT optional pointer");
    AssertEqual((byte)SccpOptionalParameterName.Segmentation, encoded[^7], "SCCP XUDT segmentation parameter name");
    AssertEqual((byte)SccpSegmentationParameter.EncodedLength, encoded[^6], "SCCP XUDT segmentation parameter length");
    AssertEqual((byte)SccpOptionalParameterName.EndOfOptionalParameters, encoded[^1], "SCCP XUDT optional end marker");

    Assert(SccpExtendedUnitdataMessage.TryDecode(encoded, out SccpExtendedUnitdataMessage? decoded, out string? error), error ?? "SCCP segmented XUDT decode failed");
    AssertEqual(0x00010203U, decoded!.Segmentation?.LocalReference, "SCCP decoded segmentation reference");
    AssertEqual((byte)1, decoded.Segmentation?.RemainingSegments, "SCCP decoded segmentation remaining");
    Assert(decoded.Segmentation?.FirstSegment == false, "SCCP decoded segmentation first flag");
}

static void SccpLudtCodecCarriesLongUserData()
{
    SccpPartyAddress called = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    SccpPartyAddress calling = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MSC, pointCode: 0x0102);
    byte[] payload = Enumerable.Range(0, 300).Select(i => (byte)i).ToArray();

    SccpLongUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1),
        hopCounter: 9,
        called,
        calling,
        payload);

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.LongUnitdata, encoded[0], "SCCP LUDT message type");
    AssertSequence([0x00, 0x08], encoded.AsSpan(3, 2), "SCCP LUDT called pointer");
    AssertSequence([0x00, 0x0C], encoded.AsSpan(5, 2), "SCCP LUDT calling pointer");
    AssertSequence([0x00, 0x10], encoded.AsSpan(7, 2), "SCCP LUDT data pointer");
    AssertSequence([0x01, 0x2C], encoded.AsSpan(23, 2), "SCCP LUDT data length");

    Assert(SccpLongUnitdataMessage.TryDecode(encoded, out SccpLongUnitdataMessage? decoded, out string? error), error ?? "SCCP LUDT decode failed");
    AssertEqual((byte)9, decoded!.HopCounter, "SCCP decoded LUDT hop counter");
    AssertSequence(payload, decoded.UserData.Span, "SCCP decoded LUDT data");
}

static void SccpUdtsCodecCarriesReturnCause()
{
    SccpPartyAddress called = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    SccpPartyAddress calling = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MSC, pointCode: 0x0102);
    SccpUnitdataServiceMessage message = new(
        SccpReturnCause.SubsystemFailure,
        called,
        calling,
        new byte[] { 0xDE, 0xAD });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.UnitdataService, encoded[0], "SCCP UDTS message type");
    AssertEqual((byte)SccpReturnCause.SubsystemFailure, encoded[1], "SCCP UDTS return cause");
    AssertEqual((byte)3, encoded[2], "SCCP UDTS called pointer");
    AssertEqual((byte)7, encoded[3], "SCCP UDTS calling pointer");
    AssertEqual((byte)11, encoded[4], "SCCP UDTS data pointer");

    Assert(SccpUnitdataServiceMessage.TryDecode(encoded, out SccpUnitdataServiceMessage? decoded, out string? error), error ?? "SCCP UDTS decode failed");
    AssertEqual(SccpReturnCause.SubsystemFailure, decoded!.ReturnCause, "SCCP decoded UDTS cause");
    AssertSequence([0xDE, 0xAD], decoded.UserData.Span, "SCCP decoded UDTS data");
}

static void SccpRouteTableResolvesSsnAndGlobalTitleRoutes()
{
    SccpRouteTable table = new();
    table.Add(new SccpRoute("map-default", SccpRouteSelector.ForSubsystem(SubsystemNumber.MAP)));
    table.Add(new SccpRoute("smsc-uk", SccpRouteSelector.ForGlobalTitlePrefix("44123")));
    table.Add(new SccpRoute("smsc-uk-specific", SccpRouteSelector.ForGlobalTitlePrefix("4412345")));

    SccpPartyAddress ssnAddress = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    Assert(table.TryResolve(ssnAddress, out SccpRoute? ssnRoute), "SCCP SSN route should resolve");
    AssertEqual("map-default", ssnRoute!.Name, "SCCP SSN route name");

    SccpPartyAddress gtAddress = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    Assert(table.TryResolve(gtAddress, out SccpRoute? gtRoute), "SCCP GT route should resolve");
    AssertEqual("smsc-uk-specific", gtRoute!.Name, "SCCP longest GT prefix route");
    AssertEqual(3, table.Snapshot().Count, "SCCP route snapshot count");
}

static void SccpReadinessReportsFoundationStatus()
{
    AssertEqual("MTP3 and SCCP foundation", SccpReadiness.ReleaseLabel, "SCCP readiness label");
    AssertEqual(6, SccpReadiness.RequiredFoundationCapabilityCount, "SCCP readiness capability count");
    Assert(
        SccpReadiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        SccpReadiness.ProductionGateDescription);

    SccpReadinessReport report = SccpReadiness.GetReport();
    Assert(report.FoundationReady, "SCCP foundation should be ready");
    Assert(!report.IsProductionReady, "SCCP should not claim production readiness without interop vectors");
    AssertEqual(6, report.FoundationCapabilityCount, "SCCP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=6/6", StringComparison.Ordinal), report.Describe());
}

static void SctpPayloadMetadataStoresStreamAndPpidValues()
{
    SctpPayloadMetadata metadata = new(streamId: 2, payloadProtocolIdentifier: 3, unordered: true);
    AssertEqual((ushort)2, metadata.StreamId, "SCTP metadata stream");
    AssertEqual((uint)3, metadata.PayloadProtocolIdentifier, "SCTP metadata PPID");
    Assert(metadata.Unordered, "SCTP metadata unordered flag");

    SctpReceiveResult result = new(bytesReceived: 12, metadata);
    AssertEqual(12, result.BytesReceived, "SCTP receive byte count");
    AssertEqual((ushort)2, result.Metadata.StreamId, "SCTP receive metadata stream");
}

static void SctpAssociationEventsDescribeLifecycleState()
{
    SctpAssociationEvent established = new(SctpAssociationEventType.Established, SctpAssociationState.Established, reason: "connected");
    AssertEqual(SctpAssociationEventType.Established, established.EventType, "SCTP event type");
    AssertEqual(SctpAssociationState.Established, established.State, "SCTP event state");
    AssertEqual("connected", established.Reason, "SCTP event reason");
}

static void SctpConnectionOptionsValidateEndpointsAndStreamCounts()
{
    SctpEndpoint remote = new("sg.example.net", 2905);
    SctpConnectionOptions options = new(
        remote,
        localEndpoint: new SctpEndpoint("0.0.0.0", 2905),
        outboundStreams: 8,
        inboundStreams: 8,
        defaultPayloadProtocolIdentifier: 3,
        connectTimeout: TimeSpan.FromSeconds(5));

    AssertEqual("sg.example.net:2905", remote.ToString(), "SCTP endpoint string");
    AssertEqual((ushort)8, options.OutboundStreams, "SCTP outbound streams");
    AssertEqual((ushort)8, options.InboundStreams, "SCTP inbound streams");
    AssertEqual((uint)3, options.DefaultPayloadProtocolIdentifier, "SCTP default PPID");
    AssertEqual(TimeSpan.FromSeconds(5), options.ConnectTimeout, "SCTP connect timeout");

    AssertThrows<ArgumentOutOfRangeException>(() => new SctpEndpoint("sg", 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpConnectionOptions(remote, outboundStreams: 0));
}

static void SctpPpidHelpersRecognizeSigtranPayloadIdentifiers()
{
    AssertEqual((uint)3, SctpPayloadProtocolIdentifiers.M3ua, "M3UA PPID");
    Assert(SctpPayloadProtocolIdentifiers.IsKnown(SctpPayloadProtocolIdentifiers.M3ua), "M3UA PPID should be known");
    Assert(SctpPayloadProtocolIdentifiers.IsKnown(SctpPayloadProtocolIdentifiers.M2pa), "M2PA PPID should be known");
    Assert(SctpPayloadProtocolIdentifiers.TryRequireKnown(SctpPayloadProtocolIdentifiers.M3ua, out string? knownError), knownError ?? "known PPID failed");
    Assert(!SctpPayloadProtocolIdentifiers.TryRequireKnown(999, out string? unknownError), "unknown PPID should fail");
    Assert(unknownError?.Contains("999", StringComparison.Ordinal) == true, unknownError ?? "missing unknown PPID error");
}

static void SctpStreamSelectionPoliciesChooseOutboundStreams()
{
    SctpStreamSelectionPolicy fixedPolicy = new(SctpStreamSelectionMode.Fixed, streamCount: 4, fixedStreamId: 2);
    AssertEqual((ushort)2, fixedPolicy.SelectStream(sequence: 99), "fixed stream selection");

    SctpStreamSelectionPolicy roundRobin = new(SctpStreamSelectionMode.RoundRobin, streamCount: 4);
    AssertEqual((ushort)0, roundRobin.SelectStream(0), "round-robin first stream");
    AssertEqual((ushort)3, roundRobin.SelectStream(7), "round-robin modulo stream");
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpStreamSelectionPolicy(streamCount: 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpStreamSelectionPolicy(streamCount: 2, fixedStreamId: 2));
}

static void SctpReconnectPoliciesComputeBoundedDelays()
{
    SctpReconnectPolicy policy = new(
        maxAttempts: 4,
        initialDelay: TimeSpan.FromMilliseconds(100),
        maxDelay: TimeSpan.FromMilliseconds(250),
        backoffMultiplier: 2.0);

    Assert(policy.IsEnabled, "reconnect should be enabled");
    AssertEqual(TimeSpan.FromMilliseconds(100), policy.GetDelay(1), "first reconnect delay");
    AssertEqual(TimeSpan.FromMilliseconds(200), policy.GetDelay(2), "second reconnect delay");
    AssertEqual(TimeSpan.FromMilliseconds(250), policy.GetDelay(3), "bounded reconnect delay");
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpReconnectPolicy(maxAttempts: -1));
    AssertThrows<ArgumentOutOfRangeException>(() => policy.GetDelay(0));
}

static void SctpTransportHealthSnapshotsExposeAssociationDetails()
{
    SctpEndpoint remote = new("sg.example.net", 2905);
    SctpEndpoint local = new("0.0.0.0", 2905);
    SctpTransportHealth health = new(
        SctpAssociationState.Established,
        remote,
        local,
        outboundStreams: 8,
        inboundStreams: 8,
        defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua,
        sentMessages: 10,
        receivedMessages: 11);

    Assert(health.IsEstablished, "SCTP health should be established");
    AssertEqual(remote, health.RemoteEndpoint, "SCTP health remote endpoint");
    AssertEqual(local, health.LocalEndpoint, "SCTP health local endpoint");
    AssertEqual(10L, health.SentMessages, "SCTP health sent messages");
    AssertEqual(11L, health.ReceivedMessages, "SCTP health received messages");
}

static void TcpSctpAdapterExposesDevelopmentMetadataAndHealth()
{
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    try
    {
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
        TcpClient client = new();
        client.Connect(IPAddress.Loopback, port);
        using TcpClient server = acceptTask.GetAwaiter().GetResult();

        using TcpSctpAdapter clientAdapter = new(client);
        using TcpSctpAdapter serverAdapter = new(server);
        ISctpMetadataSocket metadataSocket = serverAdapter;

        clientAdapter.SendAsync(new byte[] { 0x01, 0x02, 0x03 }).GetAwaiter().GetResult();
        byte[] receiveBuffer = new byte[16];
        SctpReceiveResult received = metadataSocket.ReceiveAsync(receiveBuffer).GetAwaiter().GetResult();

        AssertEqual(3, received.BytesReceived, "TCP adapter received byte count");
        AssertEqual(SctpPayloadProtocolIdentifiers.M3ua, received.Metadata.PayloadProtocolIdentifier, "TCP adapter metadata PPID");
        AssertSequence([0x01, 0x02, 0x03], receiveBuffer.AsSpan(0, received.BytesReceived), "TCP adapter received payload");

        SctpTransportHealth clientHealth = clientAdapter.GetHealthSnapshot(new SctpEndpoint("127.0.0.1", port));
        AssertEqual(1L, clientHealth.SentMessages, "TCP adapter sent health count");
        Assert(clientHealth.IsEstablished, "TCP adapter health should be established");
    }
    finally
    {
        listener.Stop();
    }
}

static void SctpTransportReadinessReportsFoundationStatus()
{
    AssertEqual("SCTP transport foundation", SctpTransportReadiness.ReleaseLabel, "SCTP readiness label");
    AssertEqual(5, SctpTransportReadiness.RequiredFoundationCapabilityCount, "SCTP foundation capability count");
    Assert(
        SctpTransportReadiness.ProductionGateDescription.Contains("Native SCTP", StringComparison.Ordinal),
        SctpTransportReadiness.ProductionGateDescription);
    SctpTransportReadinessReport report = SctpTransportReadiness.GetReport();
    AssertEqual(5, report.FoundationCapabilityCount, "Completed SCTP foundation capabilities");
    Assert(report.FoundationReady, "SCTP foundation should be ready");
    Assert(!report.IsProductionReady, "SCTP should not be production-ready without native implementation");
    Assert(report.Describe().Contains("foundationCapabilities=5/5", StringComparison.Ordinal), report.Describe());
    Assert(report.Describe().Contains("nativeSctp=False", StringComparison.Ordinal), report.Describe());
}

static void M3uaPayloadDataUsesNetworkOrder()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] payload = [0xDE, 0xAD, 0xBE, 0xEF];

    bool ok = M3uaMessageBuilder.BuildPayloadData(
        buffer,
        payload,
        opc: 0x01020304,
        dpc: 0x05060708,
        si: 3,
        ni: 2,
        mp: 1,
        sls: 15,
        out int written,
        out string? error);

    Assert(ok, error ?? "builder failed");
    AssertEqual(28, written, "total length");
    AssertSequence([0x00, 0x00, 0x00, 0x1C], buffer.Slice(4, 4), "message length");
    AssertSequence([0x02, 0x10], buffer.Slice(8, 2), "Protocol Data tag");
    AssertSequence([0x00, 0x14], buffer.Slice(10, 2), "Protocol Data parameter length");
    AssertSequence([0x01, 0x02, 0x03, 0x04], buffer.Slice(12, 4), "OPC");
    AssertSequence([0x05, 0x06, 0x07, 0x08], buffer.Slice(16, 4), "DPC");
    AssertSequence([3, 2, 1, 15], buffer.Slice(20, 4), "SI/NI/MP/SLS");
    AssertSequence(payload, buffer.Slice(24, payload.Length), "user payload");
}

static void M3uaProtocolExposesPublicMetadata()
{
    AssertEqual("M3UA", M3uaProtocol.Name, "protocol name");
    AssertEqual("RFC 4666", M3uaProtocol.Specification, "protocol specification");
    AssertEqual("Sigtran.NET", M3uaProtocol.PackageName, "package name");
    AssertEqual((byte)1, M3uaProtocol.Version, "protocol version");
    AssertEqual(8, M3uaProtocol.HeaderLength, "protocol header length");
    AssertEqual(4, M3uaProtocol.ParameterHeaderLength, "protocol parameter header length");
    Span<byte> headerBuffer = stackalloc byte[8];
    headerBuffer[0] = 2;
    headerBuffer[1] = 1;
    headerBuffer[2] = (byte)M3uaMessageClass.Management;
    headerBuffer[3] = 0x7F;
    headerBuffer[7] = 8;
    Assert(M3uaProtocol.TryReadHeader(headerBuffer, out M3uaHeaderPreview preview, out string? previewError), previewError ?? "header preview failed");
    AssertEqual((byte)2, preview.Version, "header preview version");
    AssertEqual((byte)1, preview.Reserved, "header preview reserved");
    AssertEqual(M3uaMessageClass.Management, preview.MessageClass, "header preview class");
    AssertEqual((byte)0x7F, preview.MessageType, "header preview type");
    AssertEqual((uint)8, preview.MessageLength, "header preview length");
    Assert(!M3uaProtocol.TryReadHeader([0x01, 0x00], out _, out string? shortHeaderError), "short header preview should fail");
    Assert(shortHeaderError?.Contains("too short", StringComparison.Ordinal) == true, shortHeaderError ?? "missing short header error");
    Assert(M3uaProtocol.TryValidateMessageLength(8, availableLength: 8, out string? validLengthError), validLengthError ?? "valid length failed");
    Assert(!M3uaProtocol.TryValidateMessageLength(4, availableLength: 8, out string? shortLengthError), "short message length should fail");
    Assert(shortLengthError?.Contains("Invalid M3UA length 4", StringComparison.Ordinal) == true, shortLengthError ?? "missing short length error");
    Assert(!M3uaProtocol.TryValidateMessageLength(12, availableLength: 8, out string? oversizedLengthError), "oversized message length should fail");
    Assert(oversizedLengthError?.Contains("Invalid M3UA length 12", StringComparison.Ordinal) == true, oversizedLengthError ?? "missing oversized length error");
    Assert(!M3uaProtocol.TryValidateMessageLength(10, availableLength: 12, out string? unalignedLengthError), "unaligned message length should fail");
    Assert(unalignedLengthError?.Contains("not 32-bit aligned", StringComparison.Ordinal) == true, unalignedLengthError ?? "missing unaligned length error");

    M3uaProtocolCapabilities capabilities = M3uaProtocol.Capabilities;
    Assert(capabilities.SupportsPayloadData, "capabilities should include Payload Data");
    Assert(capabilities.SupportsAspLifecycle, "capabilities should include ASP lifecycle");
    Assert(capabilities.SupportsManagement, "capabilities should include Management");
    Assert(capabilities.SupportsSsnm, "capabilities should include SSNM");
    Assert(capabilities.SupportsRkm, "capabilities should include RKM");
    Assert(capabilities.SupportsTransportSession, "capabilities should include transport session");
}

static void M3uaAlphaReadinessReportDescribesReleaseGate()
{
    AssertEqual("M3UA alpha", M3uaAlphaReadiness.ReleaseLabel, "alpha release label");
    AssertEqual(3, M3uaAlphaReadiness.RequiredVerificationCommandCount, "alpha verification command count");
    M3uaAlphaReadinessReport report = M3uaAlphaReadiness.GetReport();
    Assert(report.IsReady, "M3UA alpha readiness report should be ready");
    Assert(report.HasPackageMetadata, "alpha report package metadata");
    Assert(report.RequiresXmlDocumentation, "alpha report XML docs");
    Assert(report.HasM3uaProtocolCoverage, "alpha report protocol coverage");
    Assert(report.HasTransportAbstraction, "alpha report transport abstraction");
    Assert(report.MarksUpperLayersExperimental, "alpha report experimental upper layers");
    Assert(
        report.Describe().Contains("m3uaAlphaReady=True", StringComparison.Ordinal),
        report.Describe());
}

static void M3uaDecoderReturnsProtocolDataValue()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] payload = [0xAA, 0xBB, 0xCC];

    bool built = M3uaMessageBuilder.BuildPayloadData(
        buffer,
        payload,
        opc: 1,
        dpc: 2,
        si: 3,
        ni: 2,
        mp: 0,
        sls: 7,
        out int written,
        out string? buildError);

    Assert(built, buildError ?? "builder failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "decode failed");
    AssertEqual(M3uaMessageClass.Transfer, message.MessageClass, "message class");
    AssertEqual((byte)M3uaTransferMessageType.PayloadData, message.MessageType, "message type");
    Assert(message.TryGetParameterCount(out int parameterCount, out string? countError), countError ?? "parameter count failed");
    AssertEqual(1, parameterCount, "message parameter count");
    Assert(message.HasParameter(M3uaParameterTag.ProtocolData), "Protocol Data should be present");
    Assert(!message.HasParameter(M3uaParameterTag.HeartbeatData), "Heartbeat Data should not be present");
    Assert(message.TryGetParameter(M3uaParameterTag.ProtocolData, out ReadOnlySpan<byte> genericProtocolData, out string? genericProtocolError), genericProtocolError ?? "generic Protocol Data missing");
    Assert(message.TryGetProtocolData(out ReadOnlySpan<byte> protocolData, out string? protocolError), protocolError ?? "Protocol Data missing");

    AssertSequence(genericProtocolData, protocolData, "generic Protocol Data value");
    AssertEqual(15, protocolData.Length, "Protocol Data value length");
    AssertSequence([0x00, 0x00, 0x00, 0x01], protocolData.Slice(0, 4), "decoded OPC");
    AssertSequence([0x00, 0x00, 0x00, 0x02], protocolData.Slice(4, 4), "decoded DPC");
    AssertSequence([3, 2, 0, 7], protocolData.Slice(8, 4), "decoded SI/NI/MP/SLS");
    AssertSequence(payload, protocolData.Slice(12), "decoded user payload");
    Assert(!message.TryGetParameter(M3uaParameterTag.HeartbeatData, out _, out string? missingError), "missing Heartbeat Data should not be found");
    Assert(missingError?.Contains("HeartbeatData", StringComparison.Ordinal) == true, missingError ?? "missing parameter error");
}

static void M3uaParsesPayloadDataOptionalFields()
{
    Span<byte> buffer = stackalloc byte[80];
    byte[] payload = [0xCA, 0xFE, 0x01];

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            payload,
            opc: 0x00010203,
            dpc: 0x00040506,
            si: 3,
            ni: 2,
            mp: 1,
            sls: 8,
            networkAppearance: 0x00000007,
            routingContext: 0x00000064,
            correlationId: 0x0000002A,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    AssertEqual(52, written, "DATA length with optional fields");
    AssertSequence([0x02, 0x00, 0x00, 0x08], buffer.Slice(8, 4), "Network Appearance TLV");
    AssertSequence([0x00, 0x06, 0x00, 0x08], buffer.Slice(16, 4), "Routing Context TLV");
    AssertSequence([0x02, 0x10, 0x00, 0x13], buffer.Slice(24, 4), "Protocol Data TLV");
    AssertSequence([0x00, 0x13, 0x00, 0x08], buffer.Slice(44, 4), "Correlation Id TLV");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "DATA typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed DATA Network Appearance");
    AssertEqual((uint?)0x00000064, typed.RoutingContext, "typed DATA Routing Context");
    AssertEqual((uint)0x00010203, typed.OriginatingPointCode, "typed DATA OPC");
    AssertEqual((uint)0x00040506, typed.DestinationPointCode, "typed DATA DPC");
    AssertEqual((byte)3, typed.ServiceIndicator, "typed DATA SI");
    AssertEqual((byte)2, typed.NetworkIndicator, "typed DATA NI");
    AssertEqual((byte)1, typed.MessagePriority, "typed DATA MP");
    AssertEqual((byte)8, typed.SignallingLinkSelection, "typed DATA SLS");
    AssertSequence(payload, typed.UserPayload, "typed DATA user payload");
    AssertEqual((uint?)0x0000002A, typed.CorrelationId, "typed DATA Correlation Id");
}

static void M3uaRejectsPayloadDataWithoutProtocolData()
{
    Span<byte> buffer = stackalloc byte[16];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Transfer;
    buffer[3] = (byte)M3uaTransferMessageType.PayloadData;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer.Slice(0, 8));
    Assert(
        !M3uaTypedMessageParser.TryParsePayloadData(message, out _, out string? parseError),
        "DATA without Protocol Data should be rejected");
    Assert(parseError?.Contains("Missing Protocol Data", StringComparison.Ordinal) == true, parseError ?? "missing DATA parse error");
}

static void M3uaReportsSupportedTypedMessageKinds()
{
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Transfer, (byte)M3uaTransferMessageType.PayloadData),
        "Payload Data should be supported");
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Aspsm, (byte)M3uaAspsmMessageType.HeartbeatAck),
        "Heartbeat Ack should be supported");
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.RoutingKeyManagement, (byte)M3uaRoutingKeyManagementMessageType.RegistrationResponse),
        "Registration Response should be supported");
    Assert(
        !M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Management, 0x7F),
        "Unknown Management message should not be supported");
    Assert(
        !M3uaTypedMessageParser.IsSupported((M3uaMessageClass)0x7F, 0x01),
        "Unknown message class should not be supported");
    Assert(
        M3uaTypedMessageParser.TryRequireSupported(M3uaMessageClass.Transfer, (byte)M3uaTransferMessageType.PayloadData, out string? supportedError),
        supportedError ?? "Payload Data should be required as supported");
    AssertEqual(null, supportedError, "supported require error");
    Assert(
        !M3uaTypedMessageParser.TryRequireSupported(M3uaMessageClass.Management, 0x7F, out string? unsupportedError),
        "Unknown Management message should not be required as supported");
    Assert(unsupportedError?.Contains("class=Management type=127", StringComparison.Ordinal) == true, unsupportedError ?? "missing unsupported require error");
}

static void M3uaDispatchesKnownTypedMessages()
{
    Span<byte> buffer = stackalloc byte[128];

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? dataBuildError),
        dataBuildError ?? "DATA build failed");

    M3uaMessage dataMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(dataMessage, out M3uaTypedMessage? dataTyped, out string? dataParseError),
        dataParseError ?? "DATA dispatch failed");
    AssertEqual(M3uaTypedMessageKind.PayloadData, dataTyped!.Kind, "DATA dispatch kind");
    M3uaPayloadDataMessage payloadData = dataTyped.As<M3uaPayloadDataMessage>();
    AssertEqual((uint?)100, payloadData.RoutingContext, "DATA dispatch Routing Context");
    AssertSequence([0x01, 0x02], payloadData.UserPayload, "DATA dispatch payload");

    Assert(
        M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            new M3uaAffectedPointCode(mask: 0, pointCode: 0x00012345),
            M3uaUserPartUnavailableCause.Unknown,
            M3uaMtp3UserIdentity.Sccp,
            ReadOnlySpan<byte>.Empty,
            out written,
            out string? dupuBuildError),
        dupuBuildError ?? "DUPU build failed");

    M3uaMessage dupuMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(dupuMessage, out M3uaTypedMessage? dupuTyped, out string? dupuParseError),
        dupuParseError ?? "DUPU dispatch failed");
    AssertEqual(M3uaTypedMessageKind.DestinationUserPartUnavailable, dupuTyped!.Kind, "DUPU dispatch kind");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, dupuTyped.As<M3uaDestinationUserPartUnavailableMessage>().UserIdentity, "DUPU dispatch user identity");

    M3uaRegistrationResult[] results = [new(1, M3uaRegistrationStatus.SuccessfullyRegistered, 100)];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out written, out string? regBuildError),
        regBuildError ?? "REG RSP build failed");

    M3uaMessage registrationMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(registrationMessage, out M3uaTypedMessage? registrationTyped, out string? registrationParseError),
        registrationParseError ?? "REG RSP dispatch failed");
    AssertEqual(M3uaTypedMessageKind.RegistrationResponse, registrationTyped!.Kind, "REG RSP dispatch kind");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, registrationTyped.As<M3uaRegistrationResponseMessage>().Results[0].Status, "REG RSP dispatch status");
}

static void M3uaDispatcherRejectsUnsupportedMessageTypes()
{
    Span<byte> buffer = stackalloc byte[8];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Management;
    buffer[3] = 0x7F;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer);
    Assert(
        !M3uaTypedMessageParser.TryParseKnown(message, out _, out string? parseError),
        "unsupported Management type should be rejected");
    Assert(parseError?.Contains("Unsupported Management message type", StringComparison.Ordinal) == true, parseError ?? "missing dispatcher parse error");
}

static void M3uaRouteTableResolvesMostSpecificDataRoute()
{
    M3uaPayloadRouteTable table = new();
    Assert(table.TryAdd(new M3uaPayloadRoute("sccp-default", null, routingContext: 100, null, serviceIndicator: 3), out string? addDefaultError), addDefaultError ?? "default route add failed");
    Assert(table.TryAdd(new M3uaPayloadRoute("map-home", networkAppearance: 7, routingContext: 100, destinationPointCode: 0x00040506, serviceIndicator: 3), out string? addSpecificError), addSpecificError ?? "specific route add failed");

    M3uaPayloadDataMessage message = new(
        networkAppearance: 7,
        routingContext: 100,
        originatingPointCode: 0x00010203,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        userPayload: [0x01, 0x02],
        correlationId: null);

    Assert(table.TryResolve(message, out M3uaPayloadRoute? route, out string? resolveError), resolveError ?? "route resolve failed");
    AssertEqual("map-home", route!.Name, "resolved route name");
}

static void M3uaRouteTableRejectsAmbiguousDataRoutes()
{
    M3uaPayloadRouteTable table = new();
    Assert(table.TryAdd(new M3uaPayloadRoute("by-rc", null, routingContext: 100, null, null), out string? addRcError), addRcError ?? "RC route add failed");
    Assert(table.TryAdd(new M3uaPayloadRoute("by-dpc", null, routingContext: null, destinationPointCode: 0x00040506, null), out string? addDpcError), addDpcError ?? "DPC route add failed");

    M3uaPayloadDataMessage message = new(
        networkAppearance: null,
        routingContext: 100,
        originatingPointCode: 1,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        userPayload: ReadOnlySpan<byte>.Empty,
        correlationId: null);

    Assert(!table.TryResolve(message, out _, out string? resolveError), "ambiguous routes should be rejected");
    Assert(resolveError?.Contains("same specificity", StringComparison.Ordinal) == true, resolveError ?? "missing ambiguity error");
}

static void M3uaRouteTableRejectsDuplicateSelectors()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);

    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(!table.TryAdd(second, out string? secondError), "duplicate selectors should be rejected");
    Assert(secondError?.Contains("same selectors", StringComparison.Ordinal) == true, secondError ?? "missing duplicate route error");
}

static void M3uaRouteTableRemovesAndClearsRoutes()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: 5);

    Assert(table.IsEmpty, "new route table should be empty");
    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(table.TryAdd(second, out string? secondError), secondError ?? "second route add failed");
    AssertEqual(2, table.Count, "route table count after add");
    Assert(!table.IsEmpty, "route table should not be empty after add");

    M3uaPayloadRoute sameSelectors = new("renamed", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    Assert(table.TryRemove(sameSelectors, out string? removeError), removeError ?? "route remove failed");
    AssertEqual(1, table.Routes.Count, "route count after remove");
    AssertEqual(1, table.Count, "route table Count after remove");
    AssertEqual("second", table.Routes[0].Name, "remaining route");

    Assert(!table.TryRemove(sameSelectors, out string? missingError), "missing route remove should fail");
    Assert(missingError?.Contains("No route", StringComparison.Ordinal) == true, missingError ?? "missing route remove error");

    table.Clear();
    AssertEqual(0, table.Routes.Count, "route count after clear");
    AssertEqual(0, table.Count, "route table Count after clear");
    Assert(table.IsEmpty, "route table should be empty after clear");
}

static void M3uaRouteTableReplacesRoutesBySelector()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("old-name", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute replacement = new("new-name", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);

    Assert(table.TryAdd(first, out string? addError), addError ?? "route add failed");
    Assert(table.TryReplace(replacement, out string? replaceError), replaceError ?? "route replace failed");

    AssertEqual(1, table.Routes.Count, "route count after replace");
    AssertEqual("new-name", table.Routes[0].Name, "route name after replace");
    Assert(!table.TryFindByName("old-name", out _), "old route name should not be found after replace");
    Assert(table.TryFindByName("new-name", out _), "new route name should be found after replace");

    M3uaPayloadRoute missing = new("missing", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: null);
    Assert(!table.TryReplace(missing, out string? missingError), "missing selector replace should fail");
    Assert(missingError?.Contains("No route", StringComparison.Ordinal) == true, missingError ?? "missing replace error");
}

static void M3uaRouteTableAddsOrReplacesRoutesBySelector()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute replacement = new("replacement", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: null);

    table.AddOrReplace(first, out bool firstReplaced);
    table.AddOrReplace(replacement, out bool replacementReplaced);
    table.AddOrReplace(second, out bool secondReplaced);

    Assert(!firstReplaced, "first route should be added");
    Assert(replacementReplaced, "replacement route should replace same selectors");
    Assert(!secondReplaced, "second route should be added");
    M3uaPayloadRouteTableValidation validation = table.Validate();
    Assert(validation.IsValid, "route table should be valid");
    AssertEqual(2, validation.RouteCount, "route validation count");
    AssertEqual(0, validation.DuplicateNameCount, "route validation duplicate names");
    AssertEqual(2, table.Count, "add-or-replace route count");
    AssertEqual("replacement", table.Routes[0].Name, "replaced route name");
    AssertEqual("NA=7 RC=100 DPC=2 SI=3", table.Routes[0].DescribeSelectors(), "replaced route selectors");
    AssertEqual("second", table.Routes[1].Name, "added route name");
    AssertEqual("NA=* RC=200 DPC=* SI=*", table.Routes[1].DescribeSelectors(), "wildcard route selectors");

    table.AddOrReplace(new M3uaPayloadRoute("second", networkAppearance: null, routingContext: 201, destinationPointCode: null, serviceIndicator: null), out _);
    M3uaPayloadRouteTableValidation duplicateValidation = table.Validate();
    Assert(!duplicateValidation.IsValid, "duplicate route names should invalidate validation snapshot");
    Assert(duplicateValidation.HasDuplicateNames, "duplicate names should be reported");
}

static void M3uaRouteTableSnapshotsAndFindsRoutesByName()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: 5);

    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(table.TryAdd(second, out string? secondError), secondError ?? "second route add failed");

    M3uaPayloadRoute[] snapshot = table.Snapshot();
    AssertEqual(2, snapshot.Length, "snapshot route count");
    AssertEqual("first", snapshot[0].Name, "snapshot first route");
    table.Clear();
    AssertEqual(2, snapshot.Length, "snapshot should remain stable after clear");
    AssertEqual(0, table.Routes.Count, "table should be empty after clear");

    Assert(table.TryAdd(first, out string? readdError), readdError ?? "re-add route failed");
    Assert(table.TryFindByName("first", out M3uaPayloadRoute? found), "route by name should be found");
    AssertEqual((uint?)100, found!.RoutingContext, "found route Routing Context");
    Assert(!table.TryFindByName("missing", out _), "missing route by name should not be found");
}

static void M3uaInboundProcessorUpdatesAspStateAndRoutesData()
{
    Span<byte> buffer = stackalloc byte[128];
    M3uaPayloadRouteTable routes = new();
    Assert(
        routes.TryAdd(new M3uaPayloadRoute("map-home", networkAppearance: 7, routingContext: 100, destinationPointCode: 0x00040506, serviceIndicator: 3), out string? addError),
        addError ?? "route add failed");
    M3uaInboundProcessor processor = new(payloadRoutes: routes, requireActiveAspForPayload: true);

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? upBuildError),
        upBuildError ?? "ASP Up Ack build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? upResult, out string? upError),
        upError ?? "ASP Up Ack process failed");
    AssertEqual(M3uaAspState.Inactive, processor.AspSession.State, "processor ASP state after ASP Up Ack");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, upResult!.StateTransition!.Value.Event, "processor ASP Up event");

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, [100], ReadOnlySpan<byte>.Empty, out written, out string? activeBuildError),
        activeBuildError ?? "ASP Active Ack build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? activeResult, out string? activeError),
        activeError ?? "ASP Active Ack process failed");
    AssertEqual(M3uaAspState.Active, processor.AspSession.State, "processor ASP state after ASP Active Ack");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, activeResult!.StateTransition!.Value.Event, "processor ASP Active event");

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 0x00010203,
            dpc: 0x00040506,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: 7,
            routingContext: 100,
            correlationId: null,
            out written,
            out string? dataBuildError),
        dataBuildError ?? "DATA build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? dataResult, out string? dataError),
        dataError ?? "DATA process failed");
    AssertEqual(M3uaTypedMessageKind.PayloadData, dataResult!.TypedMessage.Kind, "processor DATA kind");
    AssertEqual("map-home", dataResult.PayloadRoute!.Name, "processor DATA route");
}

static void M3uaInboundProcessorCanRequireActiveAspForData()
{
    Span<byte> buffer = stackalloc byte[80];
    M3uaInboundProcessor processor = new(requireActiveAspForPayload: true);
    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    Assert(
        !processor.TryProcess(buffer.Slice(0, written), out _, out string? processError),
        "processor should reject DATA while ASP is not active");
    Assert(processError?.Contains("ASP is Down", StringComparison.Ordinal) == true, processError ?? "missing ASP state error");
}

static void M3uaInboundProcessorRejectsUnroutedDataWhenRoutesExist()
{
    Span<byte> buffer = stackalloc byte[80];
    M3uaPayloadRouteTable routes = new();
    Assert(routes.TryAdd(new M3uaPayloadRoute("isup", null, routingContext: 200, null, serviceIndicator: 5), out string? addError), addError ?? "route add failed");
    M3uaInboundProcessor processor = new(new M3uaAspSession(M3uaAspState.Active), routes);

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    Assert(
        !processor.TryProcess(buffer.Slice(0, written), out _, out string? processError),
        "processor should reject unrouted DATA when routes exist");
    Assert(processError?.Contains("No Payload Data route", StringComparison.Ordinal) == true, processError ?? "missing route error");
}

static void M3uaOutboundProcessorAppliesDefaultsToData()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaOutboundProcessor processor = new(networkAppearance: 7, routingContext: 100);

    Assert(
        processor.TryBuildPayloadData(
            buffer,
            userPayload: [0xAA],
            originatingPointCode: 1,
            destinationPointCode: 2,
            serviceIndicator: 3,
            networkIndicator: 2,
            messagePriority: 0,
            signallingLinkSelection: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: 42,
            out int written,
            out string? buildError),
        buildError ?? "outbound DATA build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "outbound DATA parse failed");
    AssertEqual((uint?)7, typed!.NetworkAppearance, "outbound DATA default Network Appearance");
    AssertEqual((uint?)100, typed.RoutingContext, "outbound DATA default Routing Context");
    AssertEqual((uint?)42, typed.CorrelationId, "outbound DATA Correlation Id");
    AssertSequence([0xAA], typed.UserPayload, "outbound DATA payload");

    M3uaPayloadDataMessage typedInput = new(
        networkAppearance: 8,
        routingContext: 200,
        originatingPointCode: 3,
        destinationPointCode: 4,
        serviceIndicator: 5,
        networkIndicator: 2,
        messagePriority: 1,
        signallingLinkSelection: 9,
        userPayload: [0xBB],
        correlationId: 43);
    Assert(
        processor.TryBuildPayloadData(buffer, typedInput, out written, out string? typedBuildError),
        typedBuildError ?? "typed outbound DATA build failed");

    M3uaMessage typedMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(typedMessage, out M3uaPayloadDataMessage? typedOutput, out string? typedParseError),
        typedParseError ?? "typed outbound DATA parse failed");
    AssertEqual((uint?)8, typedOutput!.NetworkAppearance, "typed outbound DATA Network Appearance");
    AssertEqual((uint?)200, typedOutput.RoutingContext, "typed outbound DATA Routing Context");
    AssertEqual((uint?)43, typedOutput.CorrelationId, "typed outbound DATA Correlation Id");
    AssertSequence([0xBB], typedOutput.UserPayload, "typed outbound DATA payload");
}

static void M3uaOutboundProcessorCanRequireActiveAspForData()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaOutboundProcessor processor = new(requireActiveAspForPayload: true);

    Assert(
        !processor.TryBuildPayloadData(
            buffer,
            userPayload: [0xAA],
            originatingPointCode: 1,
            destinationPointCode: 2,
            serviceIndicator: 3,
            networkIndicator: 2,
            messagePriority: 0,
            signallingLinkSelection: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: null,
            out _,
            out string? buildError),
        "outbound DATA should be rejected while ASP is not active");
    Assert(buildError?.Contains("ASP is Down", StringComparison.Ordinal) == true, buildError ?? "missing outbound ASP state error");
}

static void M3uaOutboundProcessorBuildsAspActiveWithDefaultRoutingContext()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaOutboundProcessor processor = new(routingContext: 100);

    Assert(
        processor.TryBuildAspActive(
            buffer,
            M3uaTrafficModeType.Loadshare,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? buildError),
        buildError ?? "outbound ASP Active build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? typed, out string? parseError),
        parseError ?? "outbound ASP Active parse failed");
    AssertEqual(M3uaAsptmMessageType.AspActive, typed!.MessageType, "outbound ASP Active type");
    AssertSequence([0x00, 0x00, 0x00, 0x64], UInt32SpanToBytes(typed.RoutingContexts), "outbound ASP Active Routing Context");
}

static void M3uaTransportSessionSendsOutboundData()
{
    FakeSctpSocket socket = new();
    M3uaOutboundProcessor outbound = new(networkAppearance: 7, routingContext: 100);
    using M3uaTransportSession session = new(socket, outboundProcessor: outbound, leaveOpen: true);

    session.SendPayloadDataAsync(
        userPayload: new byte[] { 0xCA, 0xFE },
        originatingPointCode: 1,
        destinationPointCode: 2,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        correlationId: 42).GetAwaiter().GetResult();

    AssertEqual(1, socket.SentPackets.Count, "sent packet count");
    M3uaMessage message = DecodeMessage(socket.SentPackets[0].Span);
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "sent DATA parse failed");
    AssertEqual((uint?)7, typed!.NetworkAppearance, "sent DATA Network Appearance");
    AssertEqual((uint?)100, typed.RoutingContext, "sent DATA Routing Context");
    AssertEqual((uint?)42, typed.CorrelationId, "sent DATA Correlation Id");
    AssertSequence([0xCA, 0xFE], typed.UserPayload, "sent DATA payload");

    M3uaPayloadDataMessage typedOutbound = new(
        networkAppearance: 8,
        routingContext: 200,
        originatingPointCode: 3,
        destinationPointCode: 4,
        serviceIndicator: 5,
        networkIndicator: 2,
        messagePriority: 1,
        signallingLinkSelection: 9,
        userPayload: [0xAB, 0xCD],
        correlationId: 43);
    session.SendPayloadDataAsync(typedOutbound).GetAwaiter().GetResult();

    AssertEqual(2, socket.SentPackets.Count, "typed sent packet count");
    M3uaMessage typedSent = DecodeMessage(socket.SentPackets[1].Span);
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(typedSent, out M3uaPayloadDataMessage? parsedTypedOutbound, out string? typedParseError),
        typedParseError ?? "typed sent DATA parse failed");
    AssertEqual((uint?)8, parsedTypedOutbound!.NetworkAppearance, "typed sent DATA Network Appearance");
    AssertEqual((uint?)200, parsedTypedOutbound.RoutingContext, "typed sent DATA Routing Context");
    AssertEqual((uint?)43, parsedTypedOutbound.CorrelationId, "typed sent DATA Correlation Id");
    AssertSequence([0xAB, 0xCD], parsedTypedOutbound.UserPayload, "typed sent DATA payload");
}

static void M3uaTransportSessionReceivesInboundData()
{
    Span<byte> buffer = stackalloc byte[96];
    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "inbound DATA build failed");

    FakeSctpSocket socket = new();
    socket.QueueReceive(buffer.Slice(0, written).ToArray());
    M3uaPayloadRouteTable routes = new();
    Assert(routes.TryAdd(new M3uaPayloadRoute("sccp", null, routingContext: 100, null, serviceIndicator: 3), out string? addError), addError ?? "route add failed");
    M3uaInboundProcessor inbound = new(new M3uaAspSession(M3uaAspState.Active), routes, requireActiveAspForPayload: true);
    using M3uaTransportSession session = new(socket, inboundProcessor: inbound, leaveOpen: true);

    M3uaInboundProcessingResult? result = session.ReceiveAsync().GetAwaiter().GetResult();
    Assert(result is not null, "received result should not be null");
    AssertEqual(M3uaTypedMessageKind.PayloadData, result!.TypedMessage.Kind, "received typed kind");
    AssertEqual("sccp", result.PayloadRoute!.Name, "received route");
}

static void M3uaTransportSessionWaitsForTypedMessages()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Active,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? notifyError),
        notifyError ?? "Notify build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaRegistrationResult[] results =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out written, out string? registrationError),
        registrationError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    M3uaInboundProcessingResult result = session.ReceiveUntilAsync(
        M3uaTypedMessageKind.RegistrationResponse,
        maxMessages: 2).GetAwaiter().GetResult();

    AssertEqual(M3uaTypedMessageKind.RegistrationResponse, result.TypedMessage.Kind, "waited typed kind");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, result.TypedMessage.As<M3uaRegistrationResponseMessage>().Results[0].Status, "waited registration status");
}

static void M3uaTransportSessionWaitsForAspTransitions()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Inactive,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? notifyError),
        notifyError ?? "Notify build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 0x0000002A, ReadOnlySpan<byte>.Empty, out written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new();
    M3uaInboundProcessor inbound = new(aspSession);
    using M3uaTransportSession session = new(socket, inboundProcessor: inbound, leaveOpen: true);

    M3uaInboundProcessingResult result = session.ReceiveUntilTransitionAsync(
        M3uaAspEvent.AspUpAcknowledged,
        maxMessages: 2).GetAwaiter().GetResult();

    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.StateTransition!.Value.Event, "waited ASP transition event");
    AssertEqual(M3uaAspState.Inactive, aspSession.State, "waited ASP transition state");
}

static void M3uaTransportSessionDisposesOwnedSocket()
{
    FakeSctpSocket socket = new();
    M3uaTransportSession session = new(socket);
    session.Dispose();

    Assert(socket.Disposed, "owned socket should be disposed");
}

static void M3uaTransportSessionTracksCounters()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, [0x01], out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    session.SendHeartbeatAsync(new byte[] { 0x02 }).GetAwaiter().GetResult();
    M3uaInboundProcessingResult? result = session.ReceiveAsync().GetAwaiter().GetResult();
    Assert(result is not null, "counter receive result should not be null");

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        session.SendDestinationUnavailableAsync(
            networkAppearance: null,
            routingContexts: ReadOnlyMemory<uint>.Empty,
            affectedPointCodes: ReadOnlyMemory<M3uaAffectedPointCode>.Empty,
            infoString: ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("Affected Point Code", StringComparison.Ordinal), exception.Message);

    M3uaTransportSessionCounters counters = session.Counters;
    AssertEqual(1L, counters.SentPdus, "counter sent PDUs");
    AssertEqual(1L, counters.ReceivedPdus, "counter received PDUs");
    AssertEqual(1L, counters.SendFailures, "counter send failures");
    AssertEqual(0L, counters.ReceiveFailures, "counter receive failures");

    M3uaTransportSessionHealth health = session.GetHealthSnapshot();
    AssertEqual(M3uaAspState.Down, health.AspState, "health ASP state");
    AssertEqual(1L, health.Counters.SentPdus, "health sent PDUs");
    AssertEqual(session.MaxPduSize, health.MaxPduSize, "health max PDU size");
    Assert(!health.IsDisposed, "health should show active session");
}

static void M3uaTransportSessionResetsCounters()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);
    session.SendHeartbeatAsync(new byte[] { 0x02 }).GetAwaiter().GetResult();

    M3uaTransportSessionCounters beforeReset = session.Counters;
    AssertEqual(1L, beforeReset.SentPdus, "counter sent PDUs before reset");

    session.ResetCounters();

    M3uaTransportSessionCounters afterReset = session.Counters;
    AssertEqual(0L, afterReset.SentPdus, "counter sent PDUs after reset");
    AssertEqual(0L, afterReset.ReceivedPdus, "counter received PDUs after reset");
    AssertEqual(0L, afterReset.SendFailures, "counter send failures after reset");
    AssertEqual(0L, afterReset.ReceiveFailures, "counter receive failures after reset");

    session.Dispose();
    Assert(session.GetHealthSnapshot().IsDisposed, "health should show disposed session");
}

static void M3uaTransportSessionNotifiesAspTransportLoss()
{
    FakeSctpSocket socket = new();
    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    using M3uaTransportSession session = new(socket, inbound, leaveOpen: true);

    Assert(session.TryNotifyTransportLost(out M3uaAspStateTransition transition, out string? error), error ?? "transport loss notification failed");
    AssertEqual(M3uaAspEvent.TransportLost, transition.Event, "transport loss event");
    AssertEqual(M3uaAspState.Active, transition.From, "transport loss from state");
    AssertEqual(M3uaAspState.Down, transition.To, "transport loss to state");
    AssertEqual(M3uaAspState.Down, aspSession.State, "transport loss session state");
}

static void M3uaDiagnosticsFormatHexAndSummaries()
{
    byte[] bytes =
    [
        0x00, 0x01, 0x02, 0x03,
        0x04, 0x05, 0x06, 0x07,
        0x08, 0x09
    ];
    string dump = M3uaDiagnostics.FormatHexDump(bytes, bytesPerLine: 4);
    Assert(dump.Contains("0000: 00 01 02 03", StringComparison.Ordinal), dump);
    Assert(dump.Contains("0004: 04 05 06 07", StringComparison.Ordinal), dump);
    Assert(dump.Contains("0008: 08 09", StringComparison.Ordinal), dump);

    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, [0xAA, 0xBB], out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    Assert(
        M3uaDiagnostics.TryFormatSummary(buffer.Slice(0, written), out string summary, out string? summaryError),
        summaryError ?? "summary format failed");
    AssertEqual("M3UA v1 class=Aspsm type=3 length=16 parameters=8", summary, "M3UA summary");
    Assert(
        M3uaDiagnostics.TryFormatTypedSummary(buffer.Slice(0, written), out string typedSummary, out string? typedSummaryError),
        typedSummaryError ?? "typed summary format failed");
    AssertEqual("M3UA v1 class=Aspsm type=3 kind=AspStateMaintenance length=16 parameters=8", typedSummary, "M3UA typed summary");
    Assert(
        M3uaDiagnostics.TryValidateSupportedPacket(buffer.Slice(0, written), out M3uaPacketValidationResult validation, out string? validationError),
        validationError ?? "supported packet validation failed");
    AssertEqual(M3uaMessageClass.Aspsm, validation.Header.MessageClass, "validation header class");
    AssertEqual(1, validation.ParameterCount, "validation parameter count");
    Assert(validation.DispatcherSupported, "validation dispatcher support");
    Assert(
        M3uaDiagnostics.TryFormatParameterInventory(buffer.Slice(0, written), out string inventory, out string? inventoryError),
        inventoryError ?? "parameter inventory failed");
    AssertEqual("M3UA parameters count=1 [HeartbeatData length=6 value=2 padded=8]", inventory, "M3UA parameter inventory");

    Span<byte> unsupported = stackalloc byte[8];
    unsupported[0] = 1;
    unsupported[2] = (byte)M3uaMessageClass.Management;
    unsupported[3] = 0x7F;
    unsupported[7] = 8;
    Assert(
        !M3uaDiagnostics.TryFormatTypedSummary(unsupported, out _, out string? unsupportedError),
        "unsupported typed summary should fail");
    Assert(unsupportedError?.Contains("Unsupported Management message type", StringComparison.Ordinal) == true, unsupportedError ?? "missing unsupported typed summary error");
    Assert(
        !M3uaDiagnostics.TryValidateSupportedPacket(unsupported, out _, out string? unsupportedValidationError),
        "unsupported packet validation should fail");
    Assert(unsupportedValidationError?.Contains("class=Management type=127", StringComparison.Ordinal) == true, unsupportedValidationError ?? "missing unsupported validation error");

    Assert(
        !M3uaDiagnostics.TryFormatSummary([0x01, 0x00], out _, out string? malformedError),
        "malformed summary should fail");
    Assert(malformedError?.Contains("too short", StringComparison.Ordinal) == true, malformedError ?? "missing malformed summary error");

    Span<byte> badParameter = stackalloc byte[12];
    badParameter[0] = 1;
    badParameter[2] = (byte)M3uaMessageClass.Aspsm;
    badParameter[3] = (byte)M3uaAspsmMessageType.Heartbeat;
    badParameter[7] = 12;
    badParameter[9] = (byte)M3uaParameterTag.HeartbeatData;
    badParameter[11] = 16;
    Assert(
        !M3uaDiagnostics.TryFormatParameterInventory(badParameter, out _, out string? badParameterError),
        "bad parameter inventory should fail");
    Assert(badParameterError?.Contains("exceeds remaining buffer", StringComparison.Ordinal) == true, badParameterError ?? "missing bad parameter error");

    M3uaAspSession session = new(M3uaAspState.Inactive);
    Span<byte> activeAckBuffer = stackalloc byte[64];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(activeAckBuffer, M3uaTrafficModeType.Loadshare, [100, 200], ReadOnlySpan<byte>.Empty, out int activeAckWritten, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    M3uaMessage activeAck = DecodeMessage(activeAckBuffer.Slice(0, activeAckWritten));
    Assert(session.TryApplyAcknowledgement(activeAck, out _, out string? sessionError), sessionError ?? "ASP session apply failed");
    AssertEqual("ASP state=Active aspId=none trafficMode=Loadshare routingContexts=100,200", M3uaDiagnostics.FormatAspSessionSummary(session), "ASP session summary");
}

static void M3uaAspClientCompletesStartupHandshake()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 42, ReadOnlySpan<byte>.Empty, out int written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, [100], ReadOnlySpan<byte>.Empty, out written, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new();
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 100);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaAspStartupResult result = client.StartAsync(new M3uaAspStartupOptions(
        aspIdentifier: 42,
        trafficModeType: M3uaTrafficModeType.Loadshare)).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "ASP client final state");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.AspUpAcknowledgement.StateTransition!.Value.Event, "ASP client up transition");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, result.AspActiveAcknowledgement.StateTransition!.Value.Event, "ASP client active transition");
    AssertEqual(2, socket.SentPackets.Count, "ASP client sent packet count");
    AssertEqual((byte)M3uaAspsmMessageType.AspUp, DecodeMessage(socket.SentPackets[0].Span).MessageType, "ASP client first sent type");
    AssertEqual((byte)M3uaAsptmMessageType.AspActive, DecodeMessage(socket.SentPackets[1].Span).MessageType, "ASP client second sent type");
}

static void M3uaAspStartupOptionsValidateAndDescribeSettings()
{
    M3uaAspStartupOptions options = new(
        aspIdentifier: 42,
        trafficModeType: M3uaTrafficModeType.Loadshare,
        aspUpInfoString: new byte[] { 0x01, 0x02 },
        aspActiveInfoString: new byte[] { 0x03 },
        maxHandshakeMessages: 4);

    Assert(options.TryValidate(out string? validError), validError ?? "valid options should pass");
    AssertEqual("aspId=42 trafficMode=Loadshare upInfoBytes=2 activeInfoBytes=1 maxHandshakeMessages=4", options.Describe(), "startup options description");

    M3uaAspStartupOptions tooLarge = new(aspUpInfoString: new byte[ushort.MaxValue]);
    Assert(!tooLarge.TryValidate(out string? invalidError), "oversized Info String should fail validation");
    Assert(invalidError?.Contains("exceeds M3UA parameter value limit", StringComparison.Ordinal) == true, invalidError ?? "missing oversized Info String error");
}

static void M3uaAspClientResetsBeforeStartupHandshake()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 77, ReadOnlySpan<byte>.Empty, out int written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Override, [200], ReadOnlySpan<byte>.Empty, out written, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    aspSession.Reset(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 200);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaAspStartupResult result = client.ResetAndStartAsync(new M3uaAspStartupOptions(
        aspIdentifier: 77,
        trafficModeType: M3uaTrafficModeType.Override)).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "reset ASP client final state");
    AssertEqual((uint?)77, aspSession.AspIdentifier, "reset ASP Identifier");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Override, aspSession.TrafficModeType, "reset Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0xC8], UInt32SpanToBytes(aspSession.RoutingContexts), "reset Routing Context");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.AspUpAcknowledgement.StateTransition!.Value.Event, "reset up transition");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, result.AspActiveAcknowledgement.StateTransition!.Value.Event, "reset active transition");
}

static void M3uaAspClientFailsWhenAcknowledgementIsMissing()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaAspClient client = new(transport);

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        client.StartAsync(new M3uaAspStartupOptions(maxHandshakeMessages: 1)).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("Transport closed", StringComparison.Ordinal), exception.Message);
}

static void M3uaTransportSessionSendsHeartbeat()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);

    session.SendHeartbeatAsync(new byte[] { 0x10, 0x20, 0x30 }).GetAwaiter().GetResult();

    AssertEqual(1, socket.SentPackets.Count, "Heartbeat sent packet count");
    M3uaMessage message = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Aspsm, message.MessageClass, "Heartbeat message class");
    AssertEqual((byte)M3uaAspsmMessageType.Heartbeat, message.MessageType, "Heartbeat message type");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? typed, out string? parseError),
        parseError ?? "Heartbeat parse failed");
    AssertSequence([0x10, 0x20, 0x30], typed!.HeartbeatData.Span, "Heartbeat Data");
}

static void M3uaTransportSessionAcknowledgesInboundHeartbeat()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    byte[] heartbeatData = [0x01, 0x02, 0x03];
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, heartbeatData, out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    M3uaInboundProcessingResult? result = session.ReceiveAndAcknowledgeHeartbeatAsync().GetAwaiter().GetResult();

    Assert(result is not null, "Heartbeat result should not be null");
    AssertEqual(M3uaTypedMessageKind.AspStateMaintenance, result!.TypedMessage.Kind, "Heartbeat typed kind");
    AssertEqual(1, socket.SentPackets.Count, "Heartbeat Ack sent packet count");

    M3uaMessage ack = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Aspsm, ack.MessageClass, "Heartbeat Ack class");
    AssertEqual((byte)M3uaAspsmMessageType.HeartbeatAck, ack.MessageType, "Heartbeat Ack type");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(ack, out M3uaAspStateMaintenanceMessage? typedAck, out string? parseError),
        parseError ?? "Heartbeat Ack parse failed");
    AssertSequence(heartbeatData, typedAck!.HeartbeatData.Span, "Heartbeat Ack data");
}

static void M3uaAspClientSendsHeartbeatAndWaitsForAck()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    byte[] heartbeatData = [0x01, 0x02, 0x03, 0x04];
    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, heartbeatData, out int written, out string? buildError),
        buildError ?? "Heartbeat Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaInboundProcessingResult result = client.SendHeartbeatAsync(heartbeatData).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "Heartbeat should not change ASP state");
    AssertEqual(M3uaAspEvent.HeartbeatAcknowledged, result.StateTransition!.Value.Event, "Heartbeat event");
    Assert(!result.StateTransition.Value.Changed, "Heartbeat transition should not change state");
    AssertEqual(1, socket.SentPackets.Count, "Heartbeat client sent packet count");
    M3uaMessage sent = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual((byte)M3uaAspsmMessageType.Heartbeat, sent.MessageType, "Heartbeat client sent type");
}

static void M3uaAspClientDeactivatesAndStops()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, [100], ReadOnlySpan<byte>.Empty, out int written, out string? inactiveAckError),
        inactiveAckError ?? "ASP Inactive Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspDownAck(buffer, ReadOnlySpan<byte>.Empty, out written, out string? downAckError),
        downAckError ?? "ASP Down Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 100);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaInboundProcessingResult inactive = client.DeactivateAsync().GetAwaiter().GetResult();
    M3uaInboundProcessingResult down = client.StopAsync().GetAwaiter().GetResult();

    AssertEqual(M3uaAspEvent.AspInactiveAcknowledged, inactive.StateTransition!.Value.Event, "ASP Inactive event");
    AssertEqual(M3uaAspState.Inactive, inactive.StateTransition.Value.To, "ASP Inactive final state");
    AssertEqual(M3uaAspEvent.AspDownAcknowledged, down.StateTransition!.Value.Event, "ASP Down event");
    AssertEqual(M3uaAspState.Down, aspSession.State, "ASP shutdown final state");
    AssertEqual(2, socket.SentPackets.Count, "ASP shutdown sent packet count");
    AssertEqual((byte)M3uaAsptmMessageType.AspInactive, DecodeMessage(socket.SentPackets[0].Span).MessageType, "ASP shutdown first sent type");
    AssertEqual((byte)M3uaAspsmMessageType.AspDown, DecodeMessage(socket.SentPackets[1].Span).MessageType, "ASP shutdown second sent type");
}

static void M3uaParameterReaderSkipsPadding()
{
    Span<byte> buffer = stackalloc byte[32];
    byte[] info = [0x41, 0x42, 0x43];
    byte[] routingContext = [0x00, 0x00, 0x00, 0x05];

    Assert(
        M3uaParameterWriter.TryWrite(buffer, M3uaParameterTag.InfoString, info, out int firstWritten, out string? firstError),
        firstError ?? "first parameter write failed");
    AssertEqual(8, firstWritten, "first padded length");
    Assert(
        M3uaParameterWriter.TryWrite(buffer.Slice(firstWritten), M3uaParameterTag.RoutingContext, routingContext, out int secondWritten, out string? secondError),
        secondError ?? "second parameter write failed");

    ReadOnlySpan<byte> parameters = buffer.Slice(0, firstWritten + secondWritten);
    M3uaParameterReader reader = new(parameters);

    Assert(reader.TryRead(out M3uaParameter first, out string? firstReadError), firstReadError ?? "first parameter read failed");
    AssertEqual(M3uaParameterTag.InfoString, first.Tag, "first tag");
    AssertEqual(7, first.Length, "first length excluding padding");
    AssertEqual(8, first.PaddedLength, "first padded length from reader");
    AssertSequence(info, first.Value, "first value");

    Assert(reader.TryRead(out M3uaParameter second, out string? secondReadError), secondReadError ?? "second parameter read failed");
    AssertEqual(M3uaParameterTag.RoutingContext, second.Tag, "second tag");
    AssertSequence(routingContext, second.Value, "second value");

    Assert(!reader.TryRead(out _, out string? endError), endError ?? "reader should be exhausted");
    Assert(
        M3uaParameterReader.TryFind(parameters, M3uaParameterTag.RoutingContext, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "routing context not found");
    AssertSequence(routingContext, found, "found routing context");
    Assert(M3uaParameterReader.TryCount(parameters, out int count, out string? countError), countError ?? "parameter count failed");
    AssertEqual(2, count, "parameter count");

    Span<byte> malformed = stackalloc byte[4];
    malformed[1] = (byte)M3uaParameterTag.InfoString;
    malformed[3] = 8;
    Assert(!M3uaParameterReader.TryCount(malformed, out _, out string? malformedCountError), "malformed count should fail");
    Assert(malformedCountError?.Contains("exceeds remaining buffer", StringComparison.Ordinal) == true, malformedCountError ?? "missing malformed count error");
}

static void M3uaBuildsAspUp()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] info = [0x6E, 0x6F, 0x64, 0x65];

    Assert(
        M3uaMessageBuilder.BuildAspUp(buffer, aspIdentifier: 0x01020304, info, out int written, out string? error),
        error ?? "ASP Up build failed");

    AssertEqual(24, written, "ASP Up length");
    AssertSequence([0x01, 0x00, 0x03, 0x01], buffer.Slice(0, 4), "ASP Up header");
    AssertSequence([0x00, 0x00, 0x00, 0x18], buffer.Slice(4, 4), "ASP Up message length");
    AssertSequence([0x00, 0x11, 0x00, 0x08], buffer.Slice(8, 4), "ASP Identifier TLV");
    AssertSequence([0x01, 0x02, 0x03, 0x04], buffer.Slice(12, 4), "ASP Identifier value");
    AssertSequence([0x00, 0x04, 0x00, 0x08], buffer.Slice(16, 4), "Info String TLV");
    AssertSequence(info, buffer.Slice(20, 4), "Info String value");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Up decode failed");
    AssertEqual(M3uaMessageClass.Aspsm, message.MessageClass, "ASP Up class");
    AssertEqual((byte)M3uaAspsmMessageType.AspUp, message.MessageType, "ASP Up type");
}

static void M3uaBuildsHeartbeatAck()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] heartbeatData = [0x10, 0x20, 0x30, 0x40, 0x50];

    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, heartbeatData, out int written, out string? error),
        error ?? "Heartbeat Ack build failed");

    AssertEqual(20, written, "Heartbeat Ack length");
    AssertSequence([0x01, 0x00, 0x03, 0x06], buffer.Slice(0, 4), "Heartbeat Ack header");
    AssertSequence([0x00, 0x00, 0x00, 0x14], buffer.Slice(4, 4), "Heartbeat Ack message length");
    AssertSequence([0x00, 0x09, 0x00, 0x09], buffer.Slice(8, 4), "Heartbeat Data TLV");
    AssertSequence(heartbeatData, buffer.Slice(12, heartbeatData.Length), "Heartbeat Data value");
    AssertSequence([0x00, 0x00, 0x00], buffer.Slice(17, 3), "Heartbeat Data padding");

    Assert(
        M3uaParameterReader.TryFind(buffer.Slice(8, written - 8), M3uaParameterTag.HeartbeatData, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "Heartbeat Data not found");
    AssertSequence(heartbeatData, found, "found Heartbeat Data");
}

static void M3uaBuildsAspActive()
{
    Span<byte> buffer = stackalloc byte[64];
    uint[] routingContexts = [0x01020304, 0x05060708];
    byte[] info = [0x61, 0x63, 0x74];

    Assert(
        M3uaMessageBuilder.BuildAspActive(buffer, M3uaTrafficModeType.Loadshare, routingContexts, info, out int written, out string? error),
        error ?? "ASP Active build failed");

    AssertEqual(36, written, "ASP Active length");
    AssertSequence([0x01, 0x00, 0x04, 0x01], buffer.Slice(0, 4), "ASP Active header");
    AssertSequence([0x00, 0x00, 0x00, 0x24], buffer.Slice(4, 4), "ASP Active message length");
    AssertSequence([0x00, 0x0B, 0x00, 0x08], buffer.Slice(8, 4), "Traffic Mode Type TLV");
    AssertSequence([0x00, 0x00, 0x00, 0x02], buffer.Slice(12, 4), "Traffic Mode Type value");
    AssertSequence([0x00, 0x06, 0x00, 0x0C], buffer.Slice(16, 4), "Routing Context TLV");
    AssertSequence([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08], buffer.Slice(20, 8), "Routing Context values");
    AssertSequence([0x00, 0x04, 0x00, 0x07], buffer.Slice(28, 4), "Info String TLV");
    AssertSequence(info, buffer.Slice(32, 3), "Info String value");
    AssertSequence([0x00], buffer.Slice(35, 1), "Info String padding");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Active decode failed");
    AssertEqual(M3uaMessageClass.Asptm, message.MessageClass, "ASP Active class");
    AssertEqual((byte)M3uaAsptmMessageType.AspActive, message.MessageType, "ASP Active type");
    Assert(
        M3uaParameterReader.TryFind(message.Parameters.Span, M3uaParameterTag.RoutingContext, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "Routing Context not found");
    AssertSequence([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08], found, "found Routing Context");
}

static void M3uaBuildsAspInactiveAck()
{
    Span<byte> buffer = stackalloc byte[32];
    uint[] routingContexts = [0x00000009, 0x0000000A];

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, routingContexts, ReadOnlySpan<byte>.Empty, out int written, out string? error),
        error ?? "ASP Inactive Ack build failed");

    AssertEqual(20, written, "ASP Inactive Ack length");
    AssertSequence([0x01, 0x00, 0x04, 0x04], buffer.Slice(0, 4), "ASP Inactive Ack header");
    AssertSequence([0x00, 0x00, 0x00, 0x14], buffer.Slice(4, 4), "ASP Inactive Ack message length");
    AssertSequence([0x00, 0x06, 0x00, 0x0C], buffer.Slice(8, 4), "Routing Context TLV");
    AssertSequence([0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x0A], buffer.Slice(12, 8), "Routing Context values");
}

static void M3uaParsesAspUp()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] info = [0x61, 0x73, 0x70];

    Assert(
        M3uaMessageBuilder.BuildAspUp(buffer, 0x0000002A, info, out int written, out string? buildError),
        buildError ?? "ASP Up build failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Up decode failed");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? typed, out string? parseError),
        parseError ?? "ASP Up typed parse failed");

    AssertEqual(M3uaAspsmMessageType.AspUp, typed!.MessageType, "typed ASPSM type");
    AssertEqual((uint?)0x0000002A, typed.AspIdentifier, "typed ASP Identifier");
    AssertSequence(info, typed.InfoString.Span, "typed Info String");
    AssertEqual(0, typed.HeartbeatData.Length, "typed Heartbeat Data length");
}

static void M3uaParsesAspActive()
{
    Span<byte> buffer = stackalloc byte[64];
    uint[] routingContexts = [7, 8];

    Assert(
        M3uaMessageBuilder.BuildAspActive(buffer, M3uaTrafficModeType.Override, routingContexts, ReadOnlySpan<byte>.Empty, out int written, out string? buildError),
        buildError ?? "ASP Active build failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Active decode failed");
    Assert(
        M3uaTypedMessageParser.TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? typed, out string? parseError),
        parseError ?? "ASP Active typed parse failed");

    AssertEqual(M3uaAsptmMessageType.AspActive, typed!.MessageType, "typed ASPTM type");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Override, typed.TrafficModeType, "typed Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x08], UInt32SpanToBytes(typed.RoutingContexts), "typed Routing Contexts");
}

static void M3uaRejectsMalformedTypedRoutingContext()
{
    Span<byte> buffer = stackalloc byte[32];
    byte[] malformedRoutingContext = [0x00, 0x00, 0x00];

    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Asptm;
    buffer[3] = (byte)M3uaAsptmMessageType.AspActive;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 16;
    Assert(
        M3uaParameterWriter.TryWrite(buffer.Slice(8), M3uaParameterTag.RoutingContext, malformedRoutingContext, out int parameterWritten, out string? writeError),
        writeError ?? "malformed parameter write failed");
    AssertEqual(8, parameterWritten, "malformed parameter padded length");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, 16), out string? decodeError), decodeError ?? "malformed message decode failed");
    Assert(
        !M3uaTypedMessageParser.TryParseAsptm(message, out _, out string? parseError),
        "malformed Routing Context should be rejected");
    Assert(parseError?.Contains("non-empty multiple of 4 bytes", StringComparison.Ordinal) == true, parseError ?? "missing parse error");
}

static void M3uaAspStateMachineFollowsActiveLifecycle()
{
    M3uaAspStateMachine machine = new();
    AssertEqual(M3uaAspState.Down, machine.State, "initial ASP state");

    Assert(
        machine.TryApply(M3uaAspEvent.AspUpAcknowledged, out M3uaAspStateTransition up, out string? upError),
        upError ?? "ASP Up Ack transition failed");
    AssertEqual(M3uaAspState.Down, up.From, "ASP Up Ack from");
    AssertEqual(M3uaAspState.Inactive, up.To, "ASP Up Ack to");
    Assert(up.Changed, "ASP Up Ack should change state");

    Assert(
        machine.TryApply(M3uaAspEvent.AspActiveAcknowledged, out M3uaAspStateTransition active, out string? activeError),
        activeError ?? "ASP Active Ack transition failed");
    AssertEqual(M3uaAspState.Active, machine.State, "active ASP state");
    AssertEqual(M3uaAspState.Inactive, active.From, "ASP Active Ack from");
    AssertEqual(M3uaAspState.Active, active.To, "ASP Active Ack to");

    Assert(
        machine.TryApply(M3uaAspEvent.AspInactiveAcknowledged, out M3uaAspStateTransition inactive, out string? inactiveError),
        inactiveError ?? "ASP Inactive Ack transition failed");
    AssertEqual(M3uaAspState.Inactive, machine.State, "inactive ASP state");
    AssertEqual(M3uaAspState.Active, inactive.From, "ASP Inactive Ack from");
    AssertEqual(M3uaAspState.Inactive, inactive.To, "ASP Inactive Ack to");

    Assert(
        machine.TryApply(M3uaAspEvent.AspDownAcknowledged, out M3uaAspStateTransition down, out string? downError),
        downError ?? "ASP Down Ack transition failed");
    AssertEqual(M3uaAspState.Down, machine.State, "down ASP state");
    AssertEqual(M3uaAspState.Inactive, down.From, "ASP Down Ack from");
    AssertEqual(M3uaAspState.Down, down.To, "ASP Down Ack to");
}

static void M3uaAspStateMachineRejectsInvalidTransitions()
{
    M3uaAspStateMachine machine = new();
    Assert(
        !machine.TryApply(M3uaAspEvent.AspActiveAcknowledged, out _, out string? error),
        "ASP Active Ack from Down should be rejected");
    Assert(error?.Contains("Cannot apply", StringComparison.Ordinal) == true, error ?? "missing invalid transition error");
    AssertEqual(M3uaAspState.Down, machine.State, "state after rejected transition");

    Assert(
        machine.TryApply(M3uaAspEvent.TransportLost, out M3uaAspStateTransition lost, out string? lostError),
        lostError ?? "TransportLost transition failed");
    AssertEqual(M3uaAspState.Down, lost.To, "TransportLost target");
    Assert(!lost.Changed, "TransportLost from Down should not change state");
}

static void M3uaAspSessionAppliesAcknowledgementLifecycle()
{
    M3uaAspSession session = new();
    Span<byte> buffer = stackalloc byte[64];

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? buildUpError),
        buildUpError ?? "ASP Up Ack build failed");
    M3uaMessage upAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(upAck, out M3uaAspStateTransition up, out string? upError),
        upError ?? "ASP Up Ack session apply failed");
    AssertEqual(M3uaAspState.Inactive, session.State, "session state after ASP Up Ack");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, up.Event, "ASP Up Ack event");
    AssertEqual((uint?)0x0000002A, session.AspIdentifier, "session ASP Identifier");

    uint[] routingContexts = [100, 200];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, routingContexts, ReadOnlySpan<byte>.Empty, out written, out string? buildActiveError),
        buildActiveError ?? "ASP Active Ack build failed");
    M3uaMessage activeAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(activeAck, out M3uaAspStateTransition active, out string? activeError),
        activeError ?? "ASP Active Ack session apply failed");
    AssertEqual(M3uaAspState.Active, session.State, "session state after ASP Active Ack");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, active.Event, "ASP Active Ack event");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, session.TrafficModeType, "session Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0xC8], UInt32SpanToBytes(session.RoutingContexts), "session Routing Contexts");
    Assert(session.HasRoutingContext(100), "session should have Routing Context 100");
    Assert(session.HasRoutingContext(200), "session should have Routing Context 200");
    Assert(!session.HasRoutingContext(300), "session should not have Routing Context 300");

    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, [0x01, 0x02], out written, out string? buildBeatError),
        buildBeatError ?? "Heartbeat Ack build failed");
    M3uaMessage heartbeatAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(heartbeatAck, out M3uaAspStateTransition heartbeat, out string? heartbeatError),
        heartbeatError ?? "Heartbeat Ack session apply failed");
    AssertEqual(M3uaAspState.Active, session.State, "session state after Heartbeat Ack");
    AssertEqual(M3uaAspEvent.HeartbeatAcknowledged, heartbeat.Event, "Heartbeat Ack event");
    Assert(!heartbeat.Changed, "Heartbeat Ack should not change state");

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, [100], ReadOnlySpan<byte>.Empty, out written, out string? buildInactiveError),
        buildInactiveError ?? "ASP Inactive Ack build failed");
    M3uaMessage inactiveAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(inactiveAck, out M3uaAspStateTransition inactive, out string? inactiveError),
        inactiveError ?? "ASP Inactive Ack session apply failed");
    AssertEqual(M3uaAspState.Inactive, session.State, "session state after ASP Inactive Ack");
    AssertEqual(M3uaAspEvent.AspInactiveAcknowledged, inactive.Event, "ASP Inactive Ack event");
    AssertSequence([0x00, 0x00, 0x00, 0x64], UInt32SpanToBytes(session.RoutingContexts), "session inactive Routing Contexts");

    Assert(
        M3uaMessageBuilder.BuildAspDownAck(buffer, ReadOnlySpan<byte>.Empty, out written, out string? buildDownError),
        buildDownError ?? "ASP Down Ack build failed");
    M3uaMessage downAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(downAck, out M3uaAspStateTransition down, out string? downError),
        downError ?? "ASP Down Ack session apply failed");
    AssertEqual(M3uaAspState.Down, session.State, "session state after ASP Down Ack");
    AssertEqual(M3uaAspEvent.AspDownAcknowledged, down.Event, "ASP Down Ack event");
    AssertEqual(null, session.TrafficModeType, "session Traffic Mode after ASP Down Ack");
    AssertEqual(0, session.RoutingContexts.Length, "session Routing Context count after ASP Down Ack");
    Assert(!session.HasRoutingContext(100), "session should not have Routing Context after ASP Down Ack");
}

static void M3uaAspSessionResetsNegotiatedState()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaAspSession session = new();
    uint[] routingContexts = [0x00000064];

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? buildUpError),
        buildUpError ?? "ASP Up Ack build failed");
    Assert(
        session.TryApplyAcknowledgement(DecodeMessage(buffer.Slice(0, written)), out _, out string? upError),
        upError ?? "ASP Up Ack apply failed");

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, routingContexts, ReadOnlySpan<byte>.Empty, out written, out string? buildActiveError),
        buildActiveError ?? "ASP Active Ack build failed");
    Assert(
        session.TryApplyAcknowledgement(DecodeMessage(buffer.Slice(0, written)), out _, out string? activeError),
        activeError ?? "ASP Active Ack apply failed");

    AssertEqual(M3uaAspState.Active, session.State, "session state before reset");
    AssertEqual((uint?)0x0000002A, session.AspIdentifier, "session ASP Identifier before reset");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, session.TrafficModeType, "session Traffic Mode before reset");
    AssertEqual(1, session.RoutingContexts.Length, "session Routing Context count before reset");

    session.Reset();

    AssertEqual(M3uaAspState.Down, session.State, "session state after reset");
    AssertEqual(null, session.AspIdentifier, "session ASP Identifier after reset");
    AssertEqual(null, session.TrafficModeType, "session Traffic Mode after reset");
    AssertEqual(0, session.RoutingContexts.Length, "session Routing Context count after reset");

    session.Reset(M3uaAspState.Inactive);
    AssertEqual(M3uaAspState.Inactive, session.State, "session explicit reset state");
}

static void M3uaAspSessionRejectsWrongStateAcknowledgement()
{
    M3uaAspSession session = new();
    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Override, [1], ReadOnlySpan<byte>.Empty, out int written, out string? buildError),
        buildError ?? "ASP Active Ack build failed");

    M3uaMessage activeAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        !session.TryApplyAcknowledgement(activeAck, out _, out string? error),
        "ASP Active Ack from Down should be rejected by session");
    Assert(error?.Contains("Cannot apply", StringComparison.Ordinal) == true, error ?? "missing session transition error");
    AssertEqual(M3uaAspState.Down, session.State, "session state after rejected acknowledgement");
}

static void M3uaParsesManagementError()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000011, 0x00000022];
    byte[] diagnostic = [0xDE, 0xAD, 0xBE, 0xEF, 0x01];

    Assert(
        M3uaMessageBuilder.BuildError(buffer, M3uaErrorCode.InvalidRoutingContext, routingContexts, 0x00000005, diagnostic, out int written, out string? buildError),
        buildError ?? "Error build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Management, message.MessageClass, "Error message class");
    AssertEqual((byte)M3uaManagementMessageType.Error, message.MessageType, "Error message type");
    Assert(
        M3uaTypedMessageParser.TryParseError(message, out M3uaErrorMessage? typed, out string? parseError),
        parseError ?? "Error typed parse failed");

    AssertEqual(M3uaErrorCode.InvalidRoutingContext, typed!.ErrorCode, "typed Error Code");
    AssertEqual((uint?)0x00000005, typed.NetworkAppearance, "typed Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x22], UInt32SpanToBytes(typed.RoutingContexts), "typed Error Routing Contexts");
    AssertSequence(diagnostic, typed.DiagnosticInformation.Span, "typed Diagnostic Information");
}

static void M3uaParsesManagementNotify()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000033];
    byte[] info = [0x61, 0x73, 0x2D, 0x61, 0x63, 0x74];

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Active,
            aspIdentifier: 0x0000002A,
            routingContexts,
            info,
            out int written,
            out string? buildError),
        buildError ?? "Notify build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Management, message.MessageClass, "Notify message class");
    AssertEqual((byte)M3uaManagementMessageType.Notify, message.MessageType, "Notify message type");
    Assert(
        M3uaTypedMessageParser.TryParseNotify(message, out M3uaNotifyMessage? typed, out string? parseError),
        parseError ?? "Notify typed parse failed");

    AssertEqual(M3uaNotifyStatusType.ApplicationServerStateChange, typed!.StatusType, "typed Notify Status Type");
    AssertEqual((ushort)M3uaApplicationServerState.Active, typed.StatusInformation, "typed Notify Status Information");
    AssertEqual((uint?)0x0000002A, typed.AspIdentifier, "typed Notify ASP Identifier");
    AssertSequence([0x00, 0x00, 0x00, 0x33], UInt32SpanToBytes(typed.RoutingContexts), "typed Notify Routing Contexts");
    AssertSequence(info, typed.InfoString.Span, "typed Notify Info String");
}

static void M3uaTransportSessionSendsManagementMessages()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);

    session.SendErrorAsync(
        M3uaErrorCode.InvalidRoutingContext,
        new uint[] { 0x00000011 },
        networkAppearance: 0x00000005,
        diagnosticInformation: new byte[] { 0xDE, 0xAD }).GetAwaiter().GetResult();
    session.SendNotifyAsync(
        M3uaNotifyStatusType.ApplicationServerStateChange,
        (ushort)M3uaApplicationServerState.Active,
        aspIdentifier: 0x0000002A,
        routingContexts: new uint[] { 0x00000033 },
        infoString: new byte[] { 0x61, 0x73 }).GetAwaiter().GetResult();

    AssertEqual(2, socket.SentPackets.Count, "Management sent packet count");

    M3uaMessage errorMessage = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Management, errorMessage.MessageClass, "sent Error class");
    AssertEqual((byte)M3uaManagementMessageType.Error, errorMessage.MessageType, "sent Error type");
    Assert(
        M3uaTypedMessageParser.TryParseError(errorMessage, out M3uaErrorMessage? error, out string? errorParseError),
        errorParseError ?? "sent Error parse failed");
    AssertEqual(M3uaErrorCode.InvalidRoutingContext, error!.ErrorCode, "sent Error code");
    AssertEqual((uint?)0x00000005, error.NetworkAppearance, "sent Error Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x11], UInt32SpanToBytes(error.RoutingContexts), "sent Error Routing Context");
    AssertSequence([0xDE, 0xAD], error.DiagnosticInformation.Span, "sent Error diagnostic");

    M3uaMessage notifyMessage = DecodeMessage(socket.SentPackets[1].Span);
    AssertEqual(M3uaMessageClass.Management, notifyMessage.MessageClass, "sent Notify class");
    AssertEqual((byte)M3uaManagementMessageType.Notify, notifyMessage.MessageType, "sent Notify type");
    Assert(
        M3uaTypedMessageParser.TryParseNotify(notifyMessage, out M3uaNotifyMessage? notify, out string? notifyParseError),
        notifyParseError ?? "sent Notify parse failed");
    AssertEqual(M3uaNotifyStatusType.ApplicationServerStateChange, notify!.StatusType, "sent Notify status type");
    AssertEqual((ushort)M3uaApplicationServerState.Active, notify.StatusInformation, "sent Notify status information");
    AssertEqual((uint?)0x0000002A, notify.AspIdentifier, "sent Notify ASP Identifier");
    AssertSequence([0x00, 0x00, 0x00, 0x33], UInt32SpanToBytes(notify.RoutingContexts), "sent Notify Routing Context");
    AssertSequence([0x61, 0x73], notify.InfoString.Span, "sent Notify Info String");
}

static void M3uaRejectsInvalidManagementNotifyStatusInformation()
{
    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            statusInformation: 99,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? buildError),
        buildError ?? "Notify build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        !M3uaTypedMessageParser.TryParseNotify(message, out _, out string? parseError),
        "invalid Notify status information should be rejected");
    Assert(parseError?.Contains("Unknown Notify Status Information", StringComparison.Ordinal) == true, parseError ?? "missing Notify parse error");
}

static void M3uaParsesSsnmDestinationUnavailable()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000012];
    M3uaAffectedPointCode[] affectedPointCodes =
    [
        new(mask: 0x00, pointCode: 0x00123456),
        new(mask: 0xFF, pointCode: 0x0000ABCD)
    ];
    byte[] info = [0x64, 0x75, 0x6E, 0x61];

    Assert(
        M3uaMessageBuilder.BuildDestinationUnavailable(
            buffer,
            networkAppearance: 0x00000005,
            routingContexts,
            affectedPointCodes,
            info,
            out int written,
            out string? buildError),
        buildError ?? "DUNA build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "DUNA message class");
    AssertEqual((byte)M3uaSsnmMessageType.DestinationUnavailable, message.MessageType, "DUNA message type");
    Assert(
        M3uaTypedMessageParser.TryParseSsnm(message, out M3uaSsnmMessage? typed, out string? parseError),
        parseError ?? "DUNA typed parse failed");

    AssertEqual(M3uaSsnmMessageType.DestinationUnavailable, typed!.MessageType, "typed DUNA type");
    AssertEqual((uint?)0x00000005, typed.NetworkAppearance, "typed DUNA Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x12], UInt32SpanToBytes(typed.RoutingContexts), "typed DUNA Routing Contexts");
    AssertEqual(2, typed.AffectedPointCodes.Length, "typed affected point-code count");
    AssertEqual((byte)0x00, typed.AffectedPointCodes[0].Mask, "first affected point-code mask");
    AssertEqual((uint)0x00123456, typed.AffectedPointCodes[0].PointCode, "first affected point-code value");
    AssertEqual((byte)0xFF, typed.AffectedPointCodes[1].Mask, "second affected point-code mask");
    AssertEqual((uint)0x0000ABCD, typed.AffectedPointCodes[1].PointCode, "second affected point-code value");
    AssertSequence(info, typed.InfoString.Span, "typed DUNA Info String");
}

static void M3uaRejectsSsnmWithoutAffectedPointCode()
{
    Span<byte> buffer = stackalloc byte[32];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Ssnm;
    buffer[3] = (byte)M3uaSsnmMessageType.DestinationAvailable;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer.Slice(0, 8));
    Assert(
        !M3uaTypedMessageParser.TryParseSsnm(message, out _, out string? parseError),
        "SSNM without Affected Point Code should be rejected");
    Assert(parseError?.Contains("Missing Affected Point Code", StringComparison.Ordinal) == true, parseError ?? "missing SSNM parse error");
}

static void M3uaParsesDestinationUserPartUnavailable()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000044];
    M3uaAffectedPointCode affectedPointCode = new(mask: 0, pointCode: 0x00012345);
    byte[] info = [0x64, 0x75, 0x70, 0x75];

    Assert(
        M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: 0x00000007,
            routingContexts,
            affectedPointCode,
            M3uaUserPartUnavailableCause.InaccessibleRemoteUser,
            M3uaMtp3UserIdentity.Sccp,
            info,
            out int written,
            out string? buildError),
        buildError ?? "DUPU build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "DUPU message class");
    AssertEqual((byte)M3uaSsnmMessageType.DestinationUserPartUnavailable, message.MessageType, "DUPU message type");
    Assert(
        M3uaTypedMessageParser.TryParseDestinationUserPartUnavailable(message, out M3uaDestinationUserPartUnavailableMessage? typed, out string? parseError),
        parseError ?? "DUPU typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed DUPU Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x44], UInt32SpanToBytes(typed.RoutingContexts), "typed DUPU Routing Contexts");
    AssertEqual((byte)0, typed.AffectedPointCode.Mask, "typed DUPU mask");
    AssertEqual((uint)0x00012345, typed.AffectedPointCode.PointCode, "typed DUPU point code");
    AssertEqual(M3uaUserPartUnavailableCause.InaccessibleRemoteUser, typed.Cause, "typed DUPU cause");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, typed.UserIdentity, "typed DUPU user identity");
    AssertSequence(info, typed.InfoString.Span, "typed DUPU Info String");
}

static void M3uaRejectsDupuWithNonZeroMask()
{
    Span<byte> buffer = stackalloc byte[64];
    Assert(
        !M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            new M3uaAffectedPointCode(mask: 1, pointCode: 0x00012345),
            M3uaUserPartUnavailableCause.Unknown,
            M3uaMtp3UserIdentity.Sccp,
            ReadOnlySpan<byte>.Empty,
            out _,
            out string? buildError),
        "DUPU with non-zero mask should be rejected");
    Assert(buildError?.Contains("mask must be 0", StringComparison.Ordinal) == true, buildError ?? "missing DUPU mask error");
}

static void M3uaParsesSignallingCongestion()
{
    Span<byte> buffer = stackalloc byte[96];
    uint[] routingContexts = [0x00000055];
    M3uaAffectedPointCode[] affectedPointCodes = [new(mask: 0, pointCode: 0x00112233)];
    M3uaAffectedPointCode concernedDestination = new(mask: 0, pointCode: 0x0000AAAA);
    byte[] info = [0x73, 0x63, 0x6F, 0x6E];

    Assert(
        M3uaMessageBuilder.BuildSignallingCongestion(
            buffer,
            networkAppearance: 0x00000007,
            routingContexts,
            affectedPointCodes,
            concernedDestination,
            congestionLevel: 2,
            info,
            out int written,
            out string? buildError),
        buildError ?? "SCON build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "SCON message class");
    AssertEqual((byte)M3uaSsnmMessageType.SignallingCongestion, message.MessageType, "SCON message type");
    Assert(
        M3uaTypedMessageParser.TryParseSignallingCongestion(message, out M3uaSignallingCongestionMessage? typed, out string? parseError),
        parseError ?? "SCON typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed SCON Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x55], UInt32SpanToBytes(typed.RoutingContexts), "typed SCON Routing Contexts");
    AssertEqual(1, typed.AffectedPointCodes.Length, "typed SCON affected point-code count");
    AssertEqual((byte)0, typed.AffectedPointCodes[0].Mask, "typed SCON affected point-code mask");
    AssertEqual((uint)0x00112233, typed.AffectedPointCodes[0].PointCode, "typed SCON affected point-code value");
    AssertEqual((byte)0, typed.ConcernedDestination!.Value.Mask, "typed SCON concerned destination mask");
    AssertEqual((uint)0x0000AAAA, typed.ConcernedDestination.Value.PointCode, "typed SCON concerned destination value");
    AssertEqual((uint?)2, typed.CongestionLevel, "typed SCON congestion level");
    AssertSequence(info, typed.InfoString.Span, "typed SCON Info String");
}

static void M3uaTransportSessionSendsSsnmMessages()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);
    uint[] routingContexts = [0x00000055];
    M3uaAffectedPointCode[] affectedPointCodes = [new(mask: 0, pointCode: 0x00112233)];

    session.SendDestinationUnavailableAsync(0x00000007, routingContexts, affectedPointCodes, new byte[] { 0x64, 0x75 }).GetAwaiter().GetResult();
    session.SendDestinationAvailableAsync(0x00000007, routingContexts, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationStateAuditAsync(null, ReadOnlyMemory<uint>.Empty, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationRestrictedAsync(null, ReadOnlyMemory<uint>.Empty, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationUserPartUnavailableAsync(
        0x00000007,
        routingContexts,
        new M3uaAffectedPointCode(mask: 0, pointCode: 0x00012345),
        M3uaUserPartUnavailableCause.InaccessibleRemoteUser,
        M3uaMtp3UserIdentity.Sccp,
        new byte[] { 0x64, 0x75, 0x70, 0x75 }).GetAwaiter().GetResult();
    session.SendSignallingCongestionAsync(
        0x00000007,
        routingContexts,
        affectedPointCodes,
        concernedDestination: new M3uaAffectedPointCode(mask: 0, pointCode: 0x0000AAAA),
        congestionLevel: 2,
        infoString: new byte[] { 0x73, 0x63, 0x6F, 0x6E }).GetAwaiter().GetResult();

    AssertEqual(6, socket.SentPackets.Count, "SSNM sent packet count");
    AssertCommonSsnmPacket(socket.SentPackets[0].Span, M3uaSsnmMessageType.DestinationUnavailable, "sent DUNA");
    AssertCommonSsnmPacket(socket.SentPackets[1].Span, M3uaSsnmMessageType.DestinationAvailable, "sent DAVA");
    AssertCommonSsnmPacket(socket.SentPackets[2].Span, M3uaSsnmMessageType.DestinationStateAudit, "sent DAUD");
    AssertCommonSsnmPacket(socket.SentPackets[3].Span, M3uaSsnmMessageType.DestinationRestricted, "sent DRST");

    M3uaMessage dupuMessage = DecodeMessage(socket.SentPackets[4].Span);
    Assert(
        M3uaTypedMessageParser.TryParseDestinationUserPartUnavailable(dupuMessage, out M3uaDestinationUserPartUnavailableMessage? dupu, out string? dupuError),
        dupuError ?? "sent DUPU parse failed");
    AssertEqual(M3uaUserPartUnavailableCause.InaccessibleRemoteUser, dupu!.Cause, "sent DUPU cause");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, dupu.UserIdentity, "sent DUPU user");

    M3uaMessage sconMessage = DecodeMessage(socket.SentPackets[5].Span);
    Assert(
        M3uaTypedMessageParser.TryParseSignallingCongestion(sconMessage, out M3uaSignallingCongestionMessage? scon, out string? sconError),
        sconError ?? "sent SCON parse failed");
    AssertEqual((uint?)2, scon!.CongestionLevel, "sent SCON congestion level");
    AssertEqual((uint)0x0000AAAA, scon.ConcernedDestination!.Value.PointCode, "sent SCON concerned destination");
}

static void M3uaRejectsSconWithoutAffectedPointCode()
{
    Span<byte> buffer = stackalloc byte[64];
    Assert(
        !M3uaMessageBuilder.BuildSignallingCongestion(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<M3uaAffectedPointCode>.Empty,
            concernedDestination: null,
            congestionLevel: null,
            ReadOnlySpan<byte>.Empty,
            out _,
            out string? buildError),
        "SCON without Affected Point Code should be rejected");
    Assert(buildError?.Contains("Affected Point Code", StringComparison.Ordinal) == true, buildError ?? "missing SCON affected point-code error");
}

static void M3uaParsesRegistrationRequest()
{
    Span<byte> buffer = stackalloc byte[128];
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: 0x00000064,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: 0x00000007,
            serviceIndicators: [3, 5],
            originatingPointCodes: [new M3uaAffectedPointCode(mask: 0xFF, pointCode: 0x0000ABCD)])
    ];

    Assert(
        M3uaMessageBuilder.BuildRegistrationRequest(buffer, routingKeys, out int written, out string? buildError),
        buildError ?? "REG REQ build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, message.MessageClass, "REG REQ message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationRequest, message.MessageType, "REG REQ message type");
    Assert(
        M3uaTypedMessageParser.TryParseRegistrationRequest(message, out M3uaRegistrationRequestMessage? typed, out string? parseError),
        parseError ?? "REG REQ typed parse failed");

    AssertEqual(1, typed!.RoutingKeys.Length, "typed REG REQ Routing Key count");
    M3uaRoutingKey routingKey = typed.RoutingKeys[0];
    AssertEqual((uint)0x0000002A, routingKey.LocalRoutingKeyIdentifier, "typed Local-RK-Identifier");
    AssertEqual((uint?)0x00000064, routingKey.RoutingContext, "typed Routing Context");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, routingKey.TrafficModeType, "typed Traffic Mode");
    AssertEqual((uint?)0x00000007, routingKey.NetworkAppearance, "typed Network Appearance");
    AssertEqual(1, routingKey.DestinationPointCodes.Length, "typed DPC count");
    AssertEqual((uint)0x00112233, routingKey.DestinationPointCodes[0].PointCode, "typed DPC value");
    AssertSequence([3, 5], routingKey.ServiceIndicators, "typed Service Indicators");
    AssertEqual(1, routingKey.OriginatingPointCodes.Length, "typed OPC count");
    AssertEqual((byte)0xFF, routingKey.OriginatingPointCodes[0].Mask, "typed OPC mask");
    AssertEqual((uint)0x0000ABCD, routingKey.OriginatingPointCodes[0].PointCode, "typed OPC value");
}

static void M3uaParsesRegistrationResponse()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaRegistrationResult[] results =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064),
        new(0x0000002B, M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, 0)
    ];

    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out int written, out string? buildError),
        buildError ?? "REG RSP build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, message.MessageClass, "REG RSP message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationResponse, message.MessageType, "REG RSP message type");
    Assert(
        M3uaTypedMessageParser.TryParseRegistrationResponse(message, out M3uaRegistrationResponseMessage? typed, out string? parseError),
        parseError ?? "REG RSP typed parse failed");

    AssertEqual(2, typed!.Results.Length, "typed REG RSP result count");
    AssertEqual((uint)0x0000002A, typed.Results[0].LocalRoutingKeyIdentifier, "first REG RSP Local-RK-Identifier");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, typed.Results[0].Status, "first REG RSP status");
    AssertEqual((uint)0x00000064, typed.Results[0].RoutingContext, "first REG RSP Routing Context");
    AssertEqual(M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, typed.Results[1].Status, "second REG RSP status");
}

static void M3uaParsesDeregistrationMessages()
{
    Span<byte> buffer = stackalloc byte[96];
    uint[] routingContexts = [0x00000064, 0x00000065];

    Assert(
        M3uaMessageBuilder.BuildDeregistrationRequest(buffer, routingContexts, out int written, out string? requestBuildError),
        requestBuildError ?? "DEREG REQ build failed");

    M3uaMessage request = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, request.MessageClass, "DEREG REQ message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationRequest, request.MessageType, "DEREG REQ message type");
    Assert(
        M3uaTypedMessageParser.TryParseDeregistrationRequest(request, out M3uaDeregistrationRequestMessage? typedRequest, out string? requestParseError),
        requestParseError ?? "DEREG REQ typed parse failed");
    AssertSequence([0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x65], UInt32SpanToBytes(typedRequest!.RoutingContexts), "typed DEREG REQ Routing Contexts");

    M3uaDeregistrationResult[] results =
    [
        new(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered),
        new(0x00000065, M3uaDeregistrationStatus.ErrorNotRegistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, results, out written, out string? responseBuildError),
        responseBuildError ?? "DEREG RSP build failed");

    M3uaMessage response = DecodeMessage(buffer.Slice(0, written));
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationResponse, response.MessageType, "DEREG RSP message type");
    Assert(
        M3uaTypedMessageParser.TryParseDeregistrationResponse(response, out M3uaDeregistrationResponseMessage? typedResponse, out string? responseParseError),
        responseParseError ?? "DEREG RSP typed parse failed");
    AssertEqual(2, typedResponse!.Results.Length, "typed DEREG RSP result count");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, typedResponse.Results[0].Status, "first DEREG RSP status");
    AssertEqual(M3uaDeregistrationStatus.ErrorNotRegistered, typedResponse.Results[1].Status, "second DEREG RSP status");
}

static void M3uaExposesRkmResponseConvenienceHelpers()
{
    M3uaRegistrationResponseMessage registration = new(
    [
        new M3uaRegistrationResult(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064),
        new M3uaRegistrationResult(0x0000002B, M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, 0)
    ]);
    Assert(!registration.AllSuccessful, "mixed registration results should not be all successful");
    Assert(registration.Results[0].IsSuccess, "first registration result should be success");
    Assert(!registration.Results[1].IsSuccess, "second registration result should be failure");
    Assert(registration.TryFindResult(0x0000002A, out M3uaRegistrationResult registrationResult), "registration result should be found");
    AssertEqual((uint)0x00000064, registrationResult.RoutingContext, "found registration Routing Context");
    Assert(registration.TryGetAssignedRoutingContext(0x0000002A, out uint assignedRoutingContext), "assigned Routing Context should be found");
    AssertEqual((uint)0x00000064, assignedRoutingContext, "assigned Routing Context value");
    Assert(!registration.TryGetAssignedRoutingContext(0x0000002B, out _), "failed registration should not expose assigned Routing Context");
    Assert(!registration.TryFindResult(0x0000002C, out _), "missing registration result should not be found");

    M3uaDeregistrationResponseMessage deregistration = new(
    [
        new M3uaDeregistrationResult(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered)
    ]);
    Assert(deregistration.AllSuccessful, "deregistration result should be all successful");
    Assert(deregistration.Results[0].IsSuccess, "deregistration result should be success");
    Assert(deregistration.TryFindResult(0x00000064, out M3uaDeregistrationResult deregistrationResult), "deregistration result should be found");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, deregistrationResult.Status, "found deregistration status");
    Assert(!deregistration.TryFindResult(0x00000065, out _), "missing deregistration result should not be found");
}

static void M3uaRkmClientRegistersAndDeregistersRoutingKeys()
{
    Span<byte> buffer = stackalloc byte[128];
    FakeSctpSocket socket = new();

    M3uaRegistrationResult[] registrationResults =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, registrationResults, out int written, out string? registrationBuildError),
        registrationBuildError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaDeregistrationResult[] deregistrationResults =
    [
        new(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, deregistrationResults, out written, out string? deregistrationBuildError),
        deregistrationBuildError ?? "DEREG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaRkmClient client = new(transport);
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: null,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: 0x00000007,
            serviceIndicators: [3],
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    M3uaRegistrationResponseMessage registration = client.RegisterAsync(routingKeys).GetAwaiter().GetResult();
    M3uaDeregistrationResponseMessage deregistration = client.DeregisterAsync(new uint[] { 0x00000064 }).GetAwaiter().GetResult();

    AssertEqual(1, registration.Results.Length, "RKM client REG RSP result count");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, registration.Results[0].Status, "RKM client REG RSP status");
    AssertEqual((uint)0x00000064, registration.Results[0].RoutingContext, "RKM client REG RSP Routing Context");
    AssertEqual(1, deregistration.Results.Length, "RKM client DEREG RSP result count");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, deregistration.Results[0].Status, "RKM client DEREG RSP status");
    AssertEqual(2, socket.SentPackets.Count, "RKM client sent packet count");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationRequest, DecodeMessage(socket.SentPackets[0].Span).MessageType, "RKM client first sent type");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationRequest, DecodeMessage(socket.SentPackets[1].Span).MessageType, "RKM client second sent type");
}

static void M3uaRkmClientRequiresSuccessfulResponses()
{
    Span<byte> buffer = stackalloc byte[128];
    FakeSctpSocket socket = new();

    M3uaRegistrationResult[] registrationResults =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, registrationResults, out int written, out string? registrationBuildError),
        registrationBuildError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaDeregistrationResult[] deregistrationResults =
    [
        new(0x00000064, M3uaDeregistrationStatus.ErrorNotRegistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, deregistrationResults, out written, out string? deregistrationBuildError),
        deregistrationBuildError ?? "DEREG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaRkmClient client = new(transport);
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: null,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: null,
            serviceIndicators: [3],
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    M3uaRegistrationResponseMessage registration = client.RegisterAndRequireSuccessAsync(routingKeys).GetAwaiter().GetResult();
    Assert(registration.AllSuccessful, "strict registration should return successful response");

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        client.DeregisterAndRequireSuccessAsync(new uint[] { 0x00000064 }).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("ErrorNotRegistered", StringComparison.Ordinal), exception.Message);
}

static void M3uaRejectsRoutingKeyWithoutDestinationPointCode()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 1,
            routingContext: null,
            trafficModeType: null,
            destinationPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty,
            networkAppearance: null,
            serviceIndicators: ReadOnlySpan<byte>.Empty,
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    Assert(
        !M3uaMessageBuilder.BuildRegistrationRequest(buffer, routingKeys, out _, out string? buildError),
        "Routing Key without Destination Point Code should be rejected");
    Assert(buildError?.Contains("Destination Point Code", StringComparison.Ordinal) == true, buildError ?? "missing Routing Key DPC error");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

static void AssertSequence(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, string label)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"{label}: expected {Convert.ToHexString(expected)}, got {Convert.ToHexString(actual)}");
    }
}

static void AssertCommonSsnmPacket(ReadOnlySpan<byte> packet, M3uaSsnmMessageType expectedType, string label)
{
    M3uaMessage message = DecodeMessage(packet);
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, $"{label} class");
    AssertEqual((byte)expectedType, message.MessageType, $"{label} type");
    Assert(
        M3uaTypedMessageParser.TryParseSsnm(message, out M3uaSsnmMessage? typed, out string? parseError),
        parseError ?? $"{label} parse failed");
    AssertEqual(expectedType, typed!.MessageType, $"{label} typed type");
    AssertEqual(1, typed.AffectedPointCodes.Length, $"{label} affected point-code count");
}

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException ex)
    {
        return ex;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}");
}

static byte[] UInt32SpanToBytes(ReadOnlySpan<uint> values)
{
    byte[] bytes = new byte[values.Length * sizeof(uint)];
    for (int i = 0; i < values.Length; i++)
    {
        bytes[i * sizeof(uint)] = (byte)((values[i] >> 24) & 0xFF);
        bytes[(i * sizeof(uint)) + 1] = (byte)((values[i] >> 16) & 0xFF);
        bytes[(i * sizeof(uint)) + 2] = (byte)((values[i] >> 8) & 0xFF);
        bytes[(i * sizeof(uint)) + 3] = (byte)(values[i] & 0xFF);
    }

    return bytes;
}

static M3uaMessage DecodeMessage(ReadOnlySpan<byte> encoded)
{
    M3uaMessage message = new();
    Assert(message.TryDecode(encoded, out string? error), error ?? "message decode failed");
    return message;
}

internal sealed class FakeSctpSocket : ISctpSocket
{
    private readonly Queue<byte[]> _receivePackets = new();

    public List<ReadOnlyMemory<byte>> SentPackets { get; } = new();

    public bool Disposed { get; private set; }

    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        SentPackets.Add(data.ToArray());
        return Task.CompletedTask;
    }

    public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (_receivePackets.Count == 0)
        {
            return Task.FromResult(0);
        }

        byte[] packet = _receivePackets.Dequeue();
        packet.CopyTo(buffer);
        return Task.FromResult(packet.Length);
    }

    public void QueueReceive(byte[] packet)
    {
        _receivePackets.Enqueue(packet);
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
