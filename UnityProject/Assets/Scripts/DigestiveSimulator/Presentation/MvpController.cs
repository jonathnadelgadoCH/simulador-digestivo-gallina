using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DigestiveSimulator.Data;
using DigestiveSimulator.Runtime;
using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public enum MvpScreen { MainMenu, Anatomy, Simulation, Comparison, ProjectInformation }

    [RequireComponent(typeof(DigestiveApplication), typeof(AudioNarrationManager), typeof(SpeciesModelLoader))]
    public sealed class MvpController : MonoBehaviour
    {
        private const string TextScalePreference = "DigestiveSimulator.TextScale";
        private IAnatomyView anatomy;
        private DigestiveApplication application;
        private OrbitCameraController orbitCamera;
        private AudioNarrationManager narration;
        private SpeciesModelLoader modelLoader;
        private CancellationTokenSource modelLoadCancellation;
        private OrganView selectedOrgan;
        private OrganView simulationOrgan;
        private GameObject foodParticle;
        private readonly List<Transform> nutrientMarkers = new List<Transform>();
        private readonly List<AbsorptionVisual> absorptionVisuals = new List<AbsorptionVisual>();
        private readonly List<RegulatoryVisual> regulatoryVisuals = new List<RegulatoryVisual>();
        private string nutrientMarkerFoodId;
        private Vector3 particleStart;
        private Vector3 particleTarget;
        private Vector3 particleStartScale = Vector3.one * 0.22f;
        private Vector3 particleTargetScale = Vector3.one * 0.22f;
        private float stageElapsed;
        private Vector2 foodScroll;
        private Vector2 organScroll;
        private Vector2 bibliographyScroll;
        private Vector2 comparisonScroll;
        private ReferenceDefinition[] references = System.Array.Empty<ReferenceDefinition>();
        private string errorMessage;
        private bool simulationPrepared;
        private Coroutine prepareRoutine;
        private AnatomyVisibility visibility = AnatomyVisibility.Transparent;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle wrapStyle;
        private GUIStyle statusStyle;
        private MvpScreen currentScreen = MvpScreen.MainMenu;
        private string comparisonAId;
        private string comparisonBId;
        private SpeciesFoodProfile comparisonAProfile;
        private SpeciesFoodProfile comparisonBProfile;
        private Coroutine comparisonRoutine;
        private float userTextScale = 1.1f;
        private float appliedUiScale = -1f;
        private bool showFoodProfileDetails;

        private float UiScale => Mathf.Clamp(Mathf.Min(Screen.width / 1600f, Screen.height / 900f), 1f, 1.25f) * userTextScale;
        private float LeftPanelWidth => Mathf.Clamp(300f * UiScale, 300f, 390f);
        private float RightPanelWidth => Mathf.Clamp(400f * UiScale, 400f, 520f);
        private float ContentTop => 18f + Mathf.Round(48f * UiScale);

        private void Start()
        {
            userTextScale = Mathf.Clamp(PlayerPrefs.GetFloat(TextScalePreference, 1.1f), 0.9f, 1.5f);
            application = GetComponent<DigestiveApplication>();
            narration = GetComponent<AudioNarrationManager>();
            modelLoader = GetComponent<SpeciesModelLoader>();
            application.Ready += HandleReady;
            application.Error += HandleError;
            application.Simulation.StepChanged += HandleStepChanged;
            application.Simulation.StateChanged += HandleStateChanged;
            SetupWorld();
            StartCoroutine(LoadReferences());
            if (application.IsReady) HandleReady();
        }

        private void OnDestroy()
        {
            modelLoadCancellation?.Cancel();
            modelLoadCancellation?.Dispose();
            anatomy?.Clear();
            if (application == null) return;
            application.Ready -= HandleReady;
            application.Error -= HandleError;
            application.Simulation.StepChanged -= HandleStepChanged;
            application.Simulation.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            ConfigureCameraInteraction();
            HandlePicking();
            if (!simulationPrepared || application.Simulation.State != SimulationState.Playing || application.Simulation.CurrentStep == null) return;
            stageElapsed += Time.deltaTime;
            var duration = Mathf.Max(0.1f, application.Simulation.CurrentStep.DurationSeconds);
            if (foodParticle != null)
            {
                var transition = Mathf.Clamp01(stageElapsed / Mathf.Min(1.2f, duration * 0.45f));
                foodParticle.transform.position = Vector3.Lerp(particleStart, particleTarget, transition);
                var visualScale = Vector3.Lerp(particleStartScale, particleTargetScale, transition);
                if (application.Simulation.CurrentStep.ProcessType == "mechanical_digestion")
                {
                    var compression = Mathf.Sin(Time.time * 13f) * 0.1f;
                    visualScale = Vector3.Scale(visualScale, new Vector3(1f + compression, 1f - compression, 1f + compression * 0.5f));
                }
                foodParticle.transform.localScale = visualScale;
                AnimateNutrientMarkers();
                AnimateAbsorptionVisuals();
                AnimateRegulatoryVisuals();
            }
            if (stageElapsed >= duration && narration.State != NarrationState.Loading && narration.State != NarrationState.Playing && narration.State != NarrationState.Paused)
                application.Simulation.Next();
        }

        private void ConfigureCameraInteraction()
        {
            if (orbitCamera == null) return;
            var modelVisible = currentScreen == MvpScreen.Anatomy || currentScreen == MvpScreen.Simulation;
            var bottomBoundary = currentScreen == MvpScreen.Simulation ? 82f * UiScale : 10f;
            orbitCamera.ConfigureInteraction(modelVisible, LeftPanelWidth, RightPanelWidth, ContentTop, bottomBoundary);
        }

        private void SetupWorld()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.06f, 0.09f);
            }
            orbitCamera = camera.GetComponent<OrbitCameraController>() ?? camera.gameObject.AddComponent<OrbitCameraController>();

            if (FindAnyObjectByType<Light>() == null)
            {
                var lightObject = new GameObject("Key Light");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                DontDestroyOnLoad(lightObject);
            }
        }

        private void HandleReady()
        {
            errorMessage = null;
            var species = application.SpeciesDatabase.GetAllSpecies().FirstOrDefault();
            var food = application.FoodDatabase.GetAllFoods().FirstOrDefault();
            if (species == null || food == null) { HandleError("Los catálogos no contienen una especie y un alimento utilizables."); return; }
            SelectSpecies(species.id);
            SelectFood(food.id);
            var comparisonFoods = application.FoodDatabase.GetAllFoods().OrderBy(x => x.commonName).ToArray();
            comparisonAId = comparisonFoods[0].id;
            comparisonBId = comparisonFoods.Length > 1 ? comparisonFoods[1].id : comparisonFoods[0].id;
            ReloadComparisonProfiles();
            Navigate(currentScreen);
        }

        private void SelectSpecies(string id)
        {
            if (!application.SelectSpecies(id)) return;
            modelLoadCancellation?.Cancel();
            modelLoadCancellation?.Dispose();
            ClearAbsorptionVisuals();
            ClearRegulatoryVisuals();
            anatomy?.Clear();
            var schematic = new AnatomySchematicBuilder();
            schematic.Build(application.ActiveSpecies, application.SpeciesDatabase);
            anatomy = schematic;
            anatomy.SetVisibility(visibility);
            orbitCamera.Target = anatomy.Root;
            ClearOrganSelection();
            PrepareSimulation();
            modelLoadCancellation = new CancellationTokenSource();
            _ = UpgradeToGltfModel(application.ActiveSpecies, modelLoadCancellation.Token);
        }

        private async System.Threading.Tasks.Task UpgradeToGltfModel(SpeciesDefinition species, CancellationToken cancellationToken)
        {
            LoadedSpeciesModel loaded = null;
            try
            {
                loaded = await modelLoader.Load(species, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (application.ActiveSpecies == null || application.ActiveSpecies.id != species.id) { loaded.Dispose(); return; }
                var gltfView = new GltfAnatomyView(loaded, species, application.SpeciesDatabase);
                loaded = null;
                anatomy?.Clear();
                anatomy = gltfView;
                anatomy.SetVisibility(visibility);
                anatomy.SetActive(currentScreen == MvpScreen.Anatomy || currentScreen == MvpScreen.Simulation);
                orbitCamera.Target = anatomy.Root;
                ClearOrganSelection();
                if (application.Simulation.CurrentStep != null) HandleStepChanged(application.Simulation.CurrentStep, application.Simulation.CurrentStepIndex);
            }
            catch (OperationCanceledException)
            {
                loaded?.Dispose();
            }
            catch (Exception exception)
            {
                loaded?.Dispose();
                HandleError($"No se pudo cargar el GLB; se mantiene el modelo esquemático. {exception.Message}");
            }
        }

        private void SelectFood(string id)
        {
            if (!application.SelectFood(id)) return;
            PrepareSimulation();
        }

        private void PrepareSimulation()
        {
            simulationPrepared = false;
            ResetSimulationPresentation();
            if (application.ActiveSpecies == null || application.ActiveFood == null) return;
            if (prepareRoutine != null) StopCoroutine(prepareRoutine);
            prepareRoutine = StartCoroutine(PrepareSimulationRoutine());
        }

        private IEnumerator PrepareSimulationRoutine()
        {
            yield return application.PrepareSimulation(() => simulationPrepared = true);
            prepareRoutine = null;
        }

        private void HandlePicking()
        {
            if (!Input.GetMouseButtonDown(0) || Input.mousePosition.x <= LeftPanelWidth || Input.mousePosition.x >= Screen.width - RightPanelWidth) return;
            var camera = Camera.main;
            if (camera == null) return;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit)) SelectOrgan(hit.collider.GetComponent<OrganView>());
        }

        private void SelectOrgan(OrganView organ)
        {
            if (organ == null) return;
            if (selectedOrgan != null) selectedOrgan.SetSelected(false);
            selectedOrgan = organ;
            selectedOrgan.SetSelected(true);
            orbitCamera.Focus(selectedOrgan.transform);
        }

        private void ClearOrganSelection()
        {
            if (selectedOrgan != null) selectedOrgan.SetSelected(false);
            selectedOrgan = null;
        }

        private void HandleStepChanged(SimulationStep step, int index)
        {
            if (simulationOrgan != null) simulationOrgan.SetSimulationActive(false);
            if (!anatomy.TryGetView(step.OrganId, out simulationOrgan)) return;
            simulationOrgan.SetSimulationActive(true);
            EnsureFoodParticle();
            particleStart = foodParticle != null && foodParticle.activeSelf ? foodParticle.transform.position : simulationOrgan.transform.position;
            particleTarget = simulationOrgan.transform.position;
            particleStartScale = foodParticle.activeSelf ? foodParticle.transform.localScale : Vector3.one * 0.22f;
            particleTargetScale = Vector3.one * ParticleScaleForProcess(step.ProcessType);
            stageElapsed = 0f;
            foodParticle.SetActive(visibility != AnatomyVisibility.Exterior);
            var organ = application.SpeciesDatabase.GetOrgan(application.ActiveSpecies.id, step.OrganId);
            narration.PlayNarration(!string.IsNullOrWhiteSpace(step.AudioFile) ? step.AudioFile : organ?.narrationFile);
            UpdateAbsorptionVisuals(step);
            UpdateRegulatoryVisuals(step);
        }

        private void PlayOrResumeNarration()
        {
            if (narration == null) return;
            if (narration.State == NarrationState.Paused)
            {
                narration.ResumeNarration();
                return;
            }
            if (narration.State == NarrationState.Playing || narration.State == NarrationState.Loading) return;
            if (!string.IsNullOrWhiteSpace(narration.CurrentFile))
            {
                narration.ReplayNarration();
                return;
            }

            var step = application.Simulation.CurrentStep;
            if (step == null) return;
            var organ = application.SpeciesDatabase.GetOrgan(application.ActiveSpecies.id, step.OrganId);
            narration.PlayNarration(!string.IsNullOrWhiteSpace(step.AudioFile) ? step.AudioFile : organ?.narrationFile);
        }

        private void HandleStateChanged(SimulationState state)
        {
            if (state == SimulationState.Completed && simulationOrgan != null) simulationOrgan.SetSimulationActive(false);
        }

        private void EnsureFoodParticle()
        {
            if (foodParticle == null)
            {
                foodParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foodParticle.name = "FoodParticle_Educational";
                foodParticle.transform.localScale = Vector3.one * 0.22f;
                var collider = foodParticle.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                DontDestroyOnLoad(foodParticle);
            }
            var renderer = foodParticle.GetComponent<Renderer>();
            renderer.material.color = ParseColor(application.ActiveFood?.visual?.color, new Color(1f, 0.65f, 0.15f));
            EnsureNutrientMarkers();
        }

        private void EnsureNutrientMarkers()
        {
            var foodId = application.ActiveFood?.id;
            if (nutrientMarkerFoodId == foodId) return;
            foreach (var marker in nutrientMarkers)
                if (marker != null) Destroy(marker.gameObject);
            nutrientMarkers.Clear();
            nutrientMarkerFoodId = foodId;

            var nutrients = application.Simulation?.Profile?.simulation?.highlightedNutrients ?? Array.Empty<string>();
            for (var index = 0; index < nutrients.Length; index++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"NutrientMarker_{nutrients[index]}";
                marker.transform.SetParent(foodParticle.transform, false);
                marker.transform.localScale = Vector3.one * 0.32f;
                var collider = marker.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                var markerRenderer = marker.GetComponent<Renderer>();
                markerRenderer.material.color = NutrientColor(nutrients[index]);
                nutrientMarkers.Add(marker.transform);
            }
            AnimateNutrientMarkers();
        }

        private void AnimateNutrientMarkers()
        {
            if (nutrientMarkers.Count == 0) return;
            for (var index = 0; index < nutrientMarkers.Count; index++)
            {
                var marker = nutrientMarkers[index];
                if (marker == null) continue;
                var angle = Time.unscaledTime * 55f + index * (360f / nutrientMarkers.Count);
                var radians = angle * Mathf.Deg2Rad;
                marker.localPosition = new Vector3(Mathf.Cos(radians) * 0.95f, Mathf.Sin(radians * 1.35f) * 0.42f, Mathf.Sin(radians) * 0.95f);
            }
        }

        private void ResetSimulationPresentation()
        {
            if (simulationOrgan != null) simulationOrgan.SetSimulationActive(false);
            simulationOrgan = null;
            ClearAbsorptionVisuals();
            ClearRegulatoryVisuals();
            stageElapsed = 0f;
            if (foodParticle != null)
            {
                foodParticle.transform.localScale = Vector3.one * 0.22f;
                foodParticle.SetActive(false);
            }
            if (narration != null) narration.StopNarration();
        }

        private void SetVisibility(AnatomyVisibility value)
        {
            visibility = value;
            anatomy.SetVisibility(value);
            if (foodParticle != null) foodParticle.SetActive(value != AnatomyVisibility.Exterior && application.Simulation.CurrentStep != null);
            SetAbsorptionVisualVisibility(currentScreen == MvpScreen.Simulation && value != AnatomyVisibility.Exterior);
            SetRegulatoryVisualVisibility(currentScreen == MvpScreen.Simulation && value != AnatomyVisibility.Exterior);
        }

        private void HandleError(string message) { errorMessage = message; }

        private void OnGUI()
        {
            EnsureStyles();
            if (currentScreen == MvpScreen.MainMenu) { DrawMainMenu(); return; }
            DrawTopNavigation();
            if (currentScreen == MvpScreen.Comparison) { DrawComparison(); return; }
            if (currentScreen == MvpScreen.ProjectInformation) { DrawProjectInformation(); return; }
            DrawLeftPanel();
            DrawRightPanel();
            if (currentScreen == MvpScreen.Simulation) DrawSimulationControls();
            GUI.Label(new Rect(LeftPanelWidth + 16f, ContentTop, Screen.width - LeftPanelWidth - RightPanelWidth - 32f, 52f * UiScale), "MODELO ESQUEMÁTICO PROVISIONAL — NO REPRESENTA ESCALA ANATÓMICA", statusStyle);
        }

        private void DrawMainMenu()
        {
            var width = Mathf.Min(620f, Screen.width - 40f);
            var height = Mathf.Min(560f, Screen.height - 40f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height), GUI.skin.box);
            GUILayout.FlexibleSpace();
            GUILayout.Label("PLATAFORMA DE FISIOLOGÍA DIGESTIVA", titleStyle);
            GUILayout.Label("Módulo inicial: gallina", headerStyle);
            GUILayout.Space(18f);
            GUILayout.Label("Prototipo educativo modular para explorar anatomía, alimentos y recorridos digestivos.", wrapStyle);
            GUILayout.Space(18f);
            GUI.enabled = application != null && application.IsReady;
            if (GUILayout.Button("Explorar anatomía", GUILayout.Height(42f))) Navigate(MvpScreen.Anatomy);
            if (GUILayout.Button("Simular digestión", GUILayout.Height(42f))) Navigate(MvpScreen.Simulation);
            if (GUILayout.Button("Comparar alimentos", GUILayout.Height(42f))) Navigate(MvpScreen.Comparison);
            if (GUILayout.Button("Bibliografía y proyecto", GUILayout.Height(42f))) Navigate(MvpScreen.ProjectInformation);
            GUI.enabled = true;
            DrawTextSizeControls();
            GUILayout.Space(12f);
            GUILayout.Label(application != null && application.IsReady ? "Catálogos cargados" : "Cargando catálogos…", statusStyle);
            if (!string.IsNullOrEmpty(errorMessage)) GUILayout.Label(errorMessage, wrapStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Los datos pendientes se muestran explícitamente y no se sustituyen por valores inventados.", statusStyle);
            GUILayout.EndArea();
        }

        private void DrawTopNavigation()
        {
            GUILayout.BeginArea(new Rect(10f, 8f, Screen.width - 20f, 48f * UiScale), GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Inicio")) Navigate(MvpScreen.MainMenu);
            if (GUILayout.Button("Anatomía")) Navigate(MvpScreen.Anatomy);
            if (GUILayout.Button("Simulación")) Navigate(MvpScreen.Simulation);
            if (GUILayout.Button("Comparar")) Navigate(MvpScreen.Comparison);
            if (GUILayout.Button("Proyecto")) Navigate(MvpScreen.ProjectInformation);
            GUILayout.FlexibleSpace();
            DrawTextSizeControls(true);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Navigate(MvpScreen screen)
        {
            currentScreen = screen;
            var showModel = screen == MvpScreen.Anatomy || screen == MvpScreen.Simulation;
            anatomy?.SetActive(showModel);
            if (foodParticle != null) foodParticle.SetActive(showModel && screen == MvpScreen.Simulation && visibility != AnatomyVisibility.Exterior && application.Simulation.CurrentStep != null);
            SetAbsorptionVisualVisibility(showModel && screen == MvpScreen.Simulation && visibility != AnatomyVisibility.Exterior);
            SetRegulatoryVisualVisibility(showModel && screen == MvpScreen.Simulation && visibility != AnatomyVisibility.Exterior);
            if (screen == MvpScreen.Comparison) ReloadComparisonProfiles();
        }

        private void DrawLeftPanel()
        {
            GUILayout.BeginArea(new Rect(10f, ContentTop, LeftPanelWidth - 20f, Screen.height - ContentTop - 10f), GUI.skin.box);
            GUILayout.Label("Simulador digestivo", titleStyle);
            GUILayout.Label(application == null || !application.IsReady ? "Cargando catálogos…" : "Datos modulares cargados", statusStyle);
            if (modelLoader != null) GUILayout.Label($"Modelo 3D: {modelLoader.State}", statusStyle);
            if (!string.IsNullOrEmpty(errorMessage)) GUILayout.Label(errorMessage, wrapStyle);
            if (application?.SpeciesDatabase == null) { GUILayout.EndArea(); return; }

            GUILayout.Space(8f);
            GUILayout.Label("Especie", headerStyle);
            foreach (var species in application.SpeciesDatabase.GetAllSpecies())
            {
                GUI.enabled = application.ActiveSpecies?.id != species.id;
                if (GUILayout.Button(species.commonName)) SelectSpecies(species.id);
            }
            GUI.enabled = true;

            GUILayout.Space(8f);
            GUILayout.Label("Alimento", headerStyle);
            foodScroll = GUILayout.BeginScrollView(foodScroll, GUILayout.Height(Mathf.Min(260f, Screen.height * 0.34f)));
            foreach (var food in application.FoodDatabase.GetAllFoods())
            {
                GUI.enabled = application.ActiveFood?.id != food.id;
                if (GUILayout.Button(food.commonName)) SelectFood(food.id);
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUILayout.Label("Vista", headerStyle);
            if (GUILayout.Button("Exterior")) SetVisibility(AnatomyVisibility.Exterior);
            if (GUILayout.Button("Transparente")) SetVisibility(AnatomyVisibility.Transparent);
            if (GUILayout.Button("Solo aparato digestivo")) SetVisibility(AnatomyVisibility.DigestiveOnly);
            if (GUILayout.Button("Recentrar figura")) orbitCamera?.ResetView();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Clic: seleccionar órgano\nArrastrar con botón izquierdo o derecho: girar\nBotón central o Shift + arrastre: desplazar\nRueda: acercar/alejar", wrapStyle);
            GUILayout.EndArea();
        }

        private void DrawRightPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - RightPanelWidth + 10f, ContentTop, RightPanelWidth - 20f, Screen.height - ContentTop - 10f), GUI.skin.box);
            GUILayout.Label("¿Qué está ocurriendo?", titleStyle);
            organScroll = GUILayout.BeginScrollView(organScroll);
            LabelField("Especie", application?.ActiveSpecies?.commonName);
            LabelField("Alimento", application?.ActiveFood?.commonName);
            LabelField("Estado científico", application?.Simulation?.Profile?.scientificStatus ?? application?.ActiveFood?.scientificStatus);
            if (GUILayout.Button(showFoodProfileDetails ? "▼ Ocultar diferencias del alimento" : "▶ Qué cambia con este alimento"))
                showFoodProfileDetails = !showFoodProfileDetails;
            if (showFoodProfileDetails) DrawFoodProfileDetails(application?.Simulation?.Profile);

            var step = application?.Simulation?.CurrentStep;
            if (step != null)
            {
                var stepOrgan = application.SpeciesDatabase.GetOrgan(application.ActiveSpecies.id, step.OrganId);
                GUILayout.Space(8f);
                GUILayout.Label("Simulación", headerStyle);
                LabelField("Órgano actual", GetOrganName(step.OrganId));
                LabelField("Proceso", ProcessLabel(step.ProcessType));
                LabelField(!string.IsNullOrWhiteSpace(step.Message) ? "Mensaje específico del alimento" : "Función general de la etapa", step.Message ?? stepOrgan?.physiology?.mainFunction);
                LabelField("Órganos accesorios", step.ContributingAccessoryOrganIds.Length == 0 ? "—" : string.Join(", ", step.ContributingAccessoryOrganIds.Select(GetOrganName)));
                LabelField("Etapa", $"{application.Simulation.CurrentStepIndex + 1} / {application.Simulation.Steps.Count}");
                LabelField("Narración", NarrationStatus());
                DrawNutrientLegend();
                DrawRegulatorySignals(step);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Órgano seleccionado", headerStyle);
            if (selectedOrgan?.Definition == null) GUILayout.Label("Seleccione una estructura en el modelo.", wrapStyle);
            else
            {
                var organ = selectedOrgan.Definition;
                LabelField("Nombre", organ.commonName);
                LabelField("Nombre anatómico", organ.anatomicalName);
                LabelField("Función", organ.physiology?.mainFunction);
                LabelField("Digestión mecánica", organ.physiology?.mechanicalDigestion);
                LabelField("Digestión química", organ.physiology?.chemicalDigestion);
                LabelField("Motilidad", organ.physiology?.motility);
                LabelField("Regulación", organ.regulation?.nervousControl);
                LabelField("Estado", organ.scientificStatus);
                LabelField("Referencias", ReferenceSummary(organ.referenceIds));
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawSimulationControls()
        {
            var width = Mathf.Min(700f * UiScale, Screen.width - LeftPanelWidth - RightPanelWidth - 30f);
            var height = 72f * UiScale;
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, Screen.height - height - 10f, width, height), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUI.enabled = simulationPrepared;
            if (GUILayout.Button("▶ Reproducir")) { application.Simulation.Play(); PlayOrResumeNarration(); }
            if (GUILayout.Button("⏸ Pausar")) { application.Simulation.Pause(); narration?.PauseNarration(); }
            if (GUILayout.Button("⏮ Reiniciar")) { application.Simulation.Restart(); ResetSimulationPresentation(); }
            if (GUILayout.Button("⏭ Siguiente")) application.Simulation.Next();
            if (GUILayout.Button("🔊 Escuchar")) PlayOrResumeNarration();
            if (GUILayout.Button(narration != null && narration.IsMuted ? "Audio OFF" : "Audio ON")) narration?.SetMuted(!narration.IsMuted);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(simulationPrepared
                ? $"Estado: {application.Simulation.State} · Narración: {narration?.State}"
                : "Preparando perfil especie–alimento…", statusStyle);
            GUILayout.EndArea();
        }

        private void DrawComparison()
        {
            var width = Mathf.Min(1080f, Screen.width - 40f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, ContentTop, width, Screen.height - ContentTop - 10f), GUI.skin.box);
            comparisonScroll = GUILayout.BeginScrollView(comparisonScroll);
            GUILayout.Label("Comparador de alimentos", titleStyle);
            GUILayout.Label("Los valores pertenecen al perfil de la especie activa y conservan sus condiciones de estudio.", wrapStyle);
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            DrawFoodSelector(true);
            GUILayout.Space(18f);
            DrawFoodSelector(false);
            GUILayout.EndHorizontal();
            GUILayout.Space(14f);

            var foodA = application.FoodDatabase.GetFood(comparisonAId);
            var foodB = application.FoodDatabase.GetFood(comparisonBId);
            DrawComparisonRow("Variable", foodA?.commonName, foodB?.commonName, true);
            DrawComparisonRow("Categoría", foodA?.category, foodB?.category);
            DrawComparisonRow("Materia seca", FormatNumber(foodA?.composition?.dryMatter, foodA?.composition?.units), FormatNumber(foodB?.composition?.dryMatter, foodB?.composition?.units));
            DrawComparisonRow("Proteína cruda", FormatNumber(foodA?.composition?.crudeProtein, foodA?.composition?.units), FormatNumber(foodB?.composition?.crudeProtein, foodB?.composition?.units));
            DrawComparisonRow("Almidón", FormatNumber(foodA?.composition?.starch, foodA?.composition?.units), FormatNumber(foodB?.composition?.starch, foodB?.composition?.units));
            DrawComparisonRow("Fibra cruda", FormatNumber(foodA?.composition?.crudeFiber, foodA?.composition?.units), FormatNumber(foodB?.composition?.crudeFiber, foodB?.composition?.units));
            DrawComparisonRow("Extracto etéreo", FormatNumber(foodA?.composition?.etherExtract, foodA?.composition?.units), FormatNumber(foodB?.composition?.etherExtract, foodB?.composition?.units));
            DrawComparisonRow("Digestibilidad ileal media de AA", FormatNumber(comparisonAProfile?.digestibility?.protein, comparisonAProfile?.digestibility?.units), FormatNumber(comparisonBProfile?.digestibility?.protein, comparisonBProfile?.digestibility?.units));
            DrawComparisonRow("Digestibilidad de almidón", FormatNumber(comparisonAProfile?.digestibility?.starch, comparisonAProfile?.digestibility?.units), FormatNumber(comparisonBProfile?.digestibility?.starch, comparisonBProfile?.digestibility?.units));
            DrawComparisonRow("Digestibilidad de grasa", FormatNumber(comparisonAProfile?.digestibility?.fat, comparisonAProfile?.digestibility?.units), FormatNumber(comparisonBProfile?.digestibility?.fat, comparisonBProfile?.digestibility?.units));
            DrawComparisonRow("AMEn", FormatNumber(comparisonAProfile?.digestibility?.energy, comparisonAProfile?.digestibility?.energyUnits), FormatNumber(comparisonBProfile?.digestibility?.energy, comparisonBProfile?.digestibility?.energyUnits));
            DrawComparisonRow("Método", comparisonAProfile?.digestibility?.method, comparisonBProfile?.digestibility?.method);
            DrawComparisonRow("Procesamiento", comparisonAProfile?.studyConditions?.processing, comparisonBProfile?.studyConditions?.processing);
            DrawComparisonRow("Estado científico", comparisonAProfile?.scientificStatus ?? foodA?.scientificStatus, comparisonBProfile?.scientificStatus ?? foodB?.scientificStatus);
            DrawComparisonRow("Referencias", ReferenceSummary(comparisonAProfile?.referenceIds), ReferenceSummary(comparisonBProfile?.referenceIds));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Una celda pendiente indica ausencia de una fuente validada; no equivale a cero.", statusStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawFoodSelector(bool first)
        {
            var id = first ? comparisonAId : comparisonBId;
            var food = application.FoodDatabase.GetFood(id);
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label(first ? "Alimento A" : "Alimento B", headerStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(42f))) CycleComparison(first, -1);
            GUILayout.Label(food?.commonName ?? "—", statusStyle, GUILayout.Height(28f));
            if (GUILayout.Button("▶", GUILayout.Width(42f))) CycleComparison(first, 1);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawComparisonRow(string label, string valueA, string valueB, bool header = false)
        {
            var style = header ? headerStyle : wrapStyle;
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(label, style, GUILayout.Width(245f * Mathf.Min(UiScale, 1.2f)));
            GUILayout.Label(Safe(valueA), style, GUILayout.ExpandWidth(true));
            GUILayout.Label(Safe(valueB), style, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private void CycleComparison(bool first, int delta)
        {
            var foods = application.FoodDatabase.GetAllFoods().OrderBy(x => x.commonName).ToArray();
            if (foods.Length == 0) return;
            var current = first ? comparisonAId : comparisonBId;
            var index = Array.FindIndex(foods, x => x.id == current);
            index = (index + delta + foods.Length) % foods.Length;
            if (first) comparisonAId = foods[index].id;
            else comparisonBId = foods[index].id;
            ReloadComparisonProfiles();
        }

        private void ReloadComparisonProfiles()
        {
            if (application == null || !application.IsReady || string.IsNullOrEmpty(comparisonAId) || string.IsNullOrEmpty(comparisonBId)) return;
            if (comparisonRoutine != null) StopCoroutine(comparisonRoutine);
            comparisonRoutine = StartCoroutine(LoadComparisonProfiles());
        }

        private IEnumerator LoadComparisonProfiles()
        {
            comparisonAProfile = null;
            comparisonBProfile = null;
            yield return application.LoadProfile(application.ActiveSpecies.id, comparisonAId, value => comparisonAProfile = value);
            yield return application.LoadProfile(application.ActiveSpecies.id, comparisonBId, value => comparisonBProfile = value);
            comparisonRoutine = null;
        }

        private void DrawProjectInformation()
        {
            var width = Mathf.Min(1000f, Screen.width - 40f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, ContentTop, width, Screen.height - ContentTop - 10f), GUI.skin.box);
            GUILayout.Label("Bibliografía y proyecto", titleStyle);
            GUILayout.Space(10f);
            GUILayout.Label("Estado de la bibliografía", headerStyle);
            GUILayout.Label($"La biblioteca contiene {references.Length} fuentes. Los diez ingredientes, diecinueve órganos y diez perfiles disponen de un primer borrador citado. Las fichas permanecen en estado pending hasta la revisión del equipo.", wrapStyle);
            GUILayout.Space(8f);
            bibliographyScroll = GUILayout.BeginScrollView(bibliographyScroll, GUI.skin.box, GUILayout.Height(Mathf.Max(160f, Screen.height * 0.42f)));
            foreach (var reference in references)
            {
                GUILayout.Label(FormatReference(reference), wrapStyle);
                GUILayout.Space(8f);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(12f);
            GUILayout.Label("Declaración provisional de uso de IA", headerStyle);
            GUILayout.Label("Se utilizaron herramientas de inteligencia artificial como apoyo en la arquitectura técnica, programación, depuración, organización de datos y prototipado visual. La selección e interpretación de bibliografía científica y la elaboración del guion para la defensa oral corresponden a los integrantes del proyecto.", wrapStyle);
            GUILayout.Space(16f);
            GUILayout.Label("Separación de responsabilidades", headerStyle);
            GUILayout.Label("FoodDefinition describe el ingrediente. SpeciesDefinition describe el animal. OrganDefinition describe cada órgano. SpeciesFoodProfile contiene los resultados dependientes de especie y condiciones del estudio.", wrapStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Versión técnica del contenido: 0.3.0 · Esquema: 1.0", statusStyle);
            GUILayout.EndArea();
        }

        private static string FormatNumber(float? value, string units) => value.HasValue ? $"{value.Value:0.##} {units}".Trim() : "[DATO PENDIENTE DE FUENTE CIENTÍFICA]";
        private string ReferenceSummary(string[] ids)
        {
            if (ids == null || ids.Length == 0) return "Pendientes";
            return string.Join("; ", ids.Select(id =>
            {
                var reference = references.FirstOrDefault(item => item.id == id);
                return reference == null ? id : $"{reference.authors} ({reference.year})";
            }));
        }

        private IEnumerator LoadReferences()
        {
            var client = new StreamingAssetClient();
            var url = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator/references/references.json");
            string json = null;
            yield return client.GetText(url, value => json = value, HandleError);
            if (string.IsNullOrWhiteSpace(json)) yield break;
            var library = JsonUtility.FromJson<ReferenceLibrary>(json);
            references = library?.references ?? System.Array.Empty<ReferenceDefinition>();
        }

        private static string FormatReference(ReferenceDefinition reference)
        {
            if (reference == null) return string.Empty;
            var source = !string.IsNullOrWhiteSpace(reference.journal) ? reference.journal : reference.book;
            var locator = string.Join(", ", new[] { reference.volume, reference.issue, reference.pages }.Where(value => !string.IsNullOrWhiteSpace(value)));
            var identifier = !string.IsNullOrWhiteSpace(reference.doi) ? $"doi: {reference.doi}" : reference.url;
            return $"{reference.authors} ({reference.year}). {reference.title}. {source}{(string.IsNullOrWhiteSpace(locator) ? string.Empty : ", " + locator)}. {identifier}\n{reference.url}";
        }

        private string NarrationStatus()
        {
            if (narration == null) return "No disponible";
            switch (narration.State)
            {
                case NarrationState.Missing: return "Pendiente de grabación";
                case NarrationState.Error: return narration.LastError;
                default: return narration.State.ToString();
            }
        }

        private string GetOrganName(string id)
        {
            var definition = application.SpeciesDatabase.GetOrgan(application.ActiveSpecies.id, id);
            return definition != null ? definition.commonName : id;
        }

        private void LabelField(string label, string value)
        {
            GUILayout.Label($"{label}: {Safe(value)}", wrapStyle);
        }

        private void DrawFoodProfileDetails(SpeciesFoodProfile profile)
        {
            if (profile == null)
            {
                GUILayout.Label("Preparando el perfil especie–alimento…", statusStyle);
                return;
            }
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Respuesta fisiológica documentada", headerStyle);
            LabelField("Órganos principales", string.Join(", ", (profile.physiologicalResponse?.mainOrganIds ?? Array.Empty<string>()).Select(GetOrganName)));
            LabelField("Motilidad", JoinEntries(profile.physiologicalResponse?.motilityEffects));
            LabelField("Secreciones", JoinEntries(profile.physiologicalResponse?.secretions));
            LabelField("Respuesta neuroendocrina", JoinEntries(profile.physiologicalResponse?.neuroendocrineResponses));
            LabelField("Notas metabólicas", JoinEntries(profile.physiologicalResponse?.metabolicNotes));
            GUILayout.Space(6f);
            GUILayout.Label("Condiciones de la evidencia", headerStyle);
            LabelField("Edad", profile.studyConditions?.age);
            LabelField("Línea genética", profile.studyConditions?.geneticLine);
            LabelField("Dieta", profile.studyConditions?.diet);
            LabelField("Procesamiento", profile.studyConditions?.processing);
            LabelField("Método", profile.studyConditions?.method);
            LabelField("Referencias", ReferenceSummary(profile.referenceIds));
            GUILayout.Label("Estas diferencias dependen del estudio y no describen de forma universal todos los lotes del ingrediente.", statusStyle);
            GUILayout.EndVertical();
        }

        private static string JoinEntries(string[] values)
        {
            var entries = (values ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return entries.Length == 0 ? null : string.Join("; ", entries);
        }

        private void DrawNutrientLegend()
        {
            var profile = application?.Simulation?.Profile;
            var nutrients = profile?.simulation?.highlightedNutrients ?? Array.Empty<string>();
            if (nutrients.Length == 0) return;
            GUILayout.Space(8f);
            GUILayout.Label("Nutrientes destacados", headerStyle);
            GUILayout.Label("Los colores identifican componentes del alimento; no representan una concentración cuantitativa.", wrapStyle);
            foreach (var nutrient in nutrients)
            {
                var color = ColorUtility.ToHtmlStringRGB(NutrientColor(nutrient));
                GUILayout.Label($"<color=#{color}>●</color> {nutrient}", wrapStyle);
            }
            LabelField("Coeficientes disponibles", DigestibilitySummary(profile));
            DrawAbsorptionRoutes(application.Simulation.CurrentStep, nutrients);
        }

        private void DrawAbsorptionRoutes(SimulationStep step, string[] nutrients)
        {
            var routes = RelevantAbsorptionRoutes(step, nutrients).ToArray();
            if (routes.Length == 0) return;
            GUILayout.Space(8f);
            GUILayout.Label("Rutas de absorción activas", headerStyle);
            foreach (var route in routes)
            {
                var color = ColorUtility.ToHtmlStringRGB(ParseColor(route.color, Color.white));
                GUILayout.Label($"<color=#{color}>●</color> {route.displayName}", headerStyle);
                LabelField("Forma absorbida", route.absorbedForm);
                LabelField("Transporte", route.transport);
                LabelField("Destinos generales", string.Join("; ", route.destinations ?? Array.Empty<string>()));
                LabelField("Limitación", route.caveat);
                LabelField("Fuentes", ReferenceSummary(route.referenceIds));
                GUILayout.Space(6f);
            }
        }

        private void DrawRegulatorySignals(SimulationStep step)
        {
            var organ = application?.SpeciesDatabase?.GetOrgan(application.ActiveSpecies.id, step.OrganId);
            if (organ?.regulation != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Coordinación nerviosa y endocrina", headerStyle);
                LabelField("Control nervioso", organ.regulation.nervousControl);
                LabelField("Control autónomo", organ.regulation.autonomicControl);
            }

            var signals = RelevantRegulatorySignals(step).ToArray();
            foreach (var signal in signals)
            {
                var color = ColorUtility.ToHtmlStringRGB(RegulatoryColor(signal.Signal.type));
                GUILayout.Label($"<color=#{color}>●</color> {signal.Signal.name} ({RegulatoryTypeLabel(signal.Signal.type)})", headerStyle);
                LabelField("Origen", signal.Signal.origin);
                LabelField("Receptor", signal.Signal.receptor);
                LabelField("Órgano objetivo", GetOrganName(signal.Signal.targetOrganId));
                LabelField("Respuesta", signal.Signal.response);
                LabelField("Fuente", ReferenceSummary(new[] { signal.Signal.referenceId }));
                GUILayout.Space(6f);
            }
        }

        private IEnumerable<ActiveSignal> RelevantRegulatorySignals(SimulationStep step)
        {
            if (step == null || application?.SpeciesDatabase == null) yield break;
            var organIds = new[] { step.OrganId }.Concat(step.ContributingAccessoryOrganIds ?? Array.Empty<string>());
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var organId in organIds)
            {
                var organ = application.SpeciesDatabase.GetOrgan(application.ActiveSpecies.id, organId);
                foreach (var signal in organ?.regulation?.signals ?? Array.Empty<NeuroendocrineSignal>())
                {
                    if (signal == null || string.IsNullOrWhiteSpace(signal.name) || string.IsNullOrWhiteSpace(signal.targetOrganId)) continue;
                    var key = $"{signal.name.Replace(" (CCK)", string.Empty)}|{signal.targetOrganId}";
                    if (seen.Add(key)) yield return new ActiveSignal(signal);
                }
            }
        }

        private void UpdateRegulatoryVisuals(SimulationStep step)
        {
            ClearRegulatoryVisuals();
            if (simulationOrgan == null || anatomy == null) return;
            var signals = RelevantRegulatorySignals(step).ToArray();
            for (var index = 0; index < signals.Length; index++)
            {
                var signal = signals[index].Signal;
                if (!anatomy.TryGetView(signal.targetOrganId, out var targetOrgan)) continue;
                var color = RegulatoryColor(signal.type);
                var lineObject = new GameObject($"RegulatorySignal_{signal.name}_{signal.targetOrganId}");
                var line = lineObject.AddComponent<LineRenderer>();
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    Destroy(lineObject);
                    continue;
                }
                line.material = new Material(shader);
                line.material.color = color;
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.4f);
                line.widthMultiplier = 0.026f;
                line.numCapVertices = 5;
                line.positionCount = 3;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"RegulatoryFlow_{signal.name}_{signal.targetOrganId}";
                marker.transform.localScale = Vector3.one * 0.065f;
                var collider = marker.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                marker.GetComponent<Renderer>().material.color = color;
                regulatoryVisuals.Add(new RegulatoryVisual(line, marker.transform, simulationOrgan.transform, targetOrgan.transform, index));
            }
            SetRegulatoryVisualVisibility(currentScreen == MvpScreen.Simulation && visibility != AnatomyVisibility.Exterior);
            AnimateRegulatoryVisuals();
        }

        private void AnimateRegulatoryVisuals()
        {
            foreach (var visual in regulatoryVisuals)
            {
                if (visual.Source == null || visual.Target == null) continue;
                var start = visual.Source.position;
                var end = visual.Target.position;
                var isLocalLoop = Vector3.SqrMagnitude(end - start) < 0.0001f;
                var midpoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * (0.32f + visual.OffsetIndex * 0.07f);
                if (isLocalLoop) midpoint += Vector3.right * (0.24f + visual.OffsetIndex * 0.05f);
                visual.Line.SetPosition(0, start);
                visual.Line.SetPosition(1, midpoint);
                visual.Line.SetPosition(2, end);
                var t = Mathf.Repeat(Time.time * 0.36f + visual.OffsetIndex * 0.25f, 1f);
                visual.Marker.position = QuadraticBezier(start, midpoint, end, t);
                visual.Line.widthMultiplier = 0.024f + Mathf.Sin(Time.time * 6f + visual.OffsetIndex) * 0.004f;
            }
        }

        private void ClearRegulatoryVisuals()
        {
            foreach (var visual in regulatoryVisuals)
            {
                if (visual.Line != null) Destroy(visual.Line.gameObject);
                if (visual.Marker != null) Destroy(visual.Marker.gameObject);
            }
            regulatoryVisuals.Clear();
        }

        private void SetRegulatoryVisualVisibility(bool visible)
        {
            foreach (var visual in regulatoryVisuals)
            {
                if (visual.Line != null) visual.Line.gameObject.SetActive(visible);
                if (visual.Marker != null) visual.Marker.gameObject.SetActive(visible);
            }
        }

        private static Color RegulatoryColor(string type)
        {
            switch ((type ?? string.Empty).ToLowerInvariant())
            {
                case "hormone": return new Color(1f, 0.3f, 0.75f);
                case "reflex": return new Color(0.2f, 0.95f, 0.95f);
                default: return new Color(0.7f, 0.48f, 1f);
            }
        }

        private static string RegulatoryTypeLabel(string type)
        {
            switch ((type ?? string.Empty).ToLowerInvariant())
            {
                case "hormone": return "hormona";
                case "reflex": return "reflejo";
                default: return "señal reguladora";
            }
        }

        private IEnumerable<AbsorptionRouteDefinition> RelevantAbsorptionRoutes(SimulationStep step, string[] nutrients)
        {
            if (step == null || application?.ActiveSpecies?.absorptionRoutes == null) yield break;
            foreach (var route in application.ActiveSpecies.absorptionRoutes)
            {
                if (route?.siteOrganIds == null || !route.siteOrganIds.Contains(step.OrganId, StringComparer.OrdinalIgnoreCase)) continue;
                var matchesFood = (nutrients ?? Array.Empty<string>()).Any(nutrient =>
                    (route.nutrientKeys ?? Array.Empty<string>()).Any(key => (nutrient ?? string.Empty).IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0));
                if (matchesFood) yield return route;
            }
        }

        private void UpdateAbsorptionVisuals(SimulationStep step)
        {
            ClearAbsorptionVisuals();
            if (simulationOrgan == null || anatomy == null) return;
            var nutrients = application?.Simulation?.Profile?.simulation?.highlightedNutrients ?? Array.Empty<string>();
            var routes = RelevantAbsorptionRoutes(step, nutrients).ToArray();
            for (var index = 0; index < routes.Length; index++)
            {
                var route = routes[index];
                if (!anatomy.TryGetView(route.targetOrganId, out var targetOrgan)) continue;
                var color = ParseColor(route.color, Color.white);
                var lineObject = new GameObject($"AbsorptionRoute_{route.id}");
                var line = lineObject.AddComponent<LineRenderer>();
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
                if (shader == null)
                {
                    Destroy(lineObject);
                    continue;
                }
                line.material = new Material(shader);
                line.material.color = color;
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.35f);
                line.widthMultiplier = 0.035f;
                line.numCapVertices = 5;
                line.positionCount = 3;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"AbsorptionFlow_{route.id}";
                marker.transform.localScale = Vector3.one * 0.09f;
                var collider = marker.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                marker.GetComponent<Renderer>().material.color = color;
                absorptionVisuals.Add(new AbsorptionVisual(line, marker.transform, simulationOrgan.transform, targetOrgan.transform, index));
            }
            SetAbsorptionVisualVisibility(currentScreen == MvpScreen.Simulation && visibility != AnatomyVisibility.Exterior);
            AnimateAbsorptionVisuals();
        }

        private void AnimateAbsorptionVisuals()
        {
            foreach (var visual in absorptionVisuals)
            {
                if (visual.Source == null || visual.Target == null) continue;
                var start = visual.Source.position;
                var end = visual.Target.position;
                var midpoint = Vector3.Lerp(start, end, 0.5f) + Vector3.up * (0.25f + visual.OffsetIndex * 0.08f);
                visual.Line.SetPosition(0, start);
                visual.Line.SetPosition(1, midpoint);
                visual.Line.SetPosition(2, end);
                var t = Mathf.Repeat(Time.time * 0.28f + visual.OffsetIndex * 0.22f, 1f);
                visual.Marker.position = QuadraticBezier(start, midpoint, end, t);
                visual.Line.widthMultiplier = 0.032f + Mathf.Sin(Time.time * 5f + visual.OffsetIndex) * 0.006f;
            }
        }

        private void ClearAbsorptionVisuals()
        {
            foreach (var visual in absorptionVisuals)
            {
                if (visual.Line != null) Destroy(visual.Line.gameObject);
                if (visual.Marker != null) Destroy(visual.Marker.gameObject);
            }
            absorptionVisuals.Clear();
        }

        private void SetAbsorptionVisualVisibility(bool visible)
        {
            foreach (var visual in absorptionVisuals)
            {
                if (visual.Line != null) visual.Line.gameObject.SetActive(visible);
                if (visual.Marker != null) visual.Marker.gameObject.SetActive(visible);
            }
        }

        private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            var inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private sealed class AbsorptionVisual
        {
            public readonly LineRenderer Line;
            public readonly Transform Marker;
            public readonly Transform Source;
            public readonly Transform Target;
            public readonly int OffsetIndex;

            public AbsorptionVisual(LineRenderer line, Transform marker, Transform source, Transform target, int offsetIndex)
            {
                Line = line;
                Marker = marker;
                Source = source;
                Target = target;
                OffsetIndex = offsetIndex;
            }
        }

        private sealed class ActiveSignal
        {
            public readonly NeuroendocrineSignal Signal;
            public ActiveSignal(NeuroendocrineSignal signal) { Signal = signal; }
        }

        private sealed class RegulatoryVisual
        {
            public readonly LineRenderer Line;
            public readonly Transform Marker;
            public readonly Transform Source;
            public readonly Transform Target;
            public readonly int OffsetIndex;

            public RegulatoryVisual(LineRenderer line, Transform marker, Transform source, Transform target, int offsetIndex)
            {
                Line = line;
                Marker = marker;
                Source = source;
                Target = target;
                OffsetIndex = offsetIndex;
            }
        }

        private static string DigestibilitySummary(SpeciesFoodProfile profile)
        {
            if (profile?.digestibility == null) return "Sin coeficientes cuantitativos para este perfil";
            var values = new List<string>();
            if (profile.digestibility.protein.HasValue) values.Add($"AA ileales {profile.digestibility.protein.Value:0.##}{profile.digestibility.units}");
            if (profile.digestibility.starch.HasValue) values.Add($"almidón {profile.digestibility.starch.Value:0.##}{profile.digestibility.units}");
            if (profile.digestibility.fat.HasValue) values.Add($"grasa {profile.digestibility.fat.Value:0.##}{profile.digestibility.units}");
            if (profile.digestibility.energy.HasValue) values.Add($"AMEn {profile.digestibility.energy.Value:0.##} {profile.digestibility.energyUnits}");
            return values.Count == 0 ? "Sin coeficientes cuantitativos para este perfil" : string.Join(" · ", values);
        }

        private static Color NutrientColor(string nutrient)
        {
            var key = (nutrient ?? string.Empty).ToLowerInvariant();
            if (key.Contains("almid") || key.Contains("energ")) return new Color(1f, 0.78f, 0.18f);
            if (key.Contains("prote") || key.Contains("amino")) return new Color(0.25f, 0.72f, 1f);
            if (key.Contains("lípid") || key.Contains("lipid") || key.Contains("linole")) return new Color(1f, 0.42f, 0.22f);
            if (key.Contains("fibra") || key.Contains("glucan") || key.Contains("polisac") || key.Contains("oligosac")) return new Color(0.32f, 0.86f, 0.46f);
            if (key.Contains("carot")) return new Color(1f, 0.58f, 0.12f);
            if (key.Contains("tanino")) return new Color(0.64f, 0.38f, 0.2f);
            return new Color(0.78f, 0.62f, 1f);
        }

        private static float ParticleScaleForProcess(string process)
        {
            switch (process)
            {
                case "ingestion": return 0.22f;
                case "initial_processing": return 0.215f;
                case "storage": return 0.23f;
                case "chemical_digestion": return 0.19f;
                case "mechanical_digestion": return 0.145f;
                case "digestion_absorption": return 0.12f;
                case "absorption": return 0.09f;
                case "fermentation": return 0.075f;
                case "water_recovery": return 0.065f;
                case "elimination": return 0.06f;
                default: return 0.21f;
            }
        }

        private static string ProcessLabel(string process)
        {
            switch (process)
            {
                case "ingestion": return "Ingesta";
                case "initial_processing": return "Procesamiento inicial";
                case "transit": return "Transporte del contenido";
                case "storage": return "Almacenamiento y humectación";
                case "chemical_digestion": return "Digestión química";
                case "mechanical_digestion": return "Trituración mecánica";
                case "digestion_absorption": return "Digestión y comienzo de absorción";
                case "absorption": return "Absorción intestinal";
                case "fermentation": return "Fermentación limitada";
                case "water_recovery": return "Recuperación de agua";
                case "elimination": return "Eliminación";
                default: return "Etapa digestiva";
            }
        }

        private static string Safe(string value) => string.IsNullOrWhiteSpace(value) ? "[DATO PENDIENTE DE FUENTE CIENTÍFICA]" : value;

        private void DrawTextSizeControls(bool compact = false)
        {
            GUILayout.BeginHorizontal();
            if (!compact) GUILayout.Label("Tamaño del texto", statusStyle);
            if (GUILayout.Button("A−", GUILayout.Width(48f * UiScale))) AdjustTextScale(-0.1f);
            GUILayout.Label($"{Mathf.RoundToInt(userTextScale * 100f)} %", statusStyle, GUILayout.Width(58f * UiScale));
            if (GUILayout.Button("A+", GUILayout.Width(48f * UiScale))) AdjustTextScale(0.1f);
            GUILayout.EndHorizontal();
        }

        private void AdjustTextScale(float delta)
        {
            userTextScale = Mathf.Clamp(Mathf.Round((userTextScale + delta) * 10f) / 10f, 0.9f, 1.5f);
            PlayerPrefs.SetFloat(TextScalePreference, userTextScale);
            PlayerPrefs.Save();
            appliedUiScale = -1f;
        }

        private void EnsureStyles()
        {
            var scale = UiScale;
            if (titleStyle != null && Mathf.Abs(appliedUiScale - scale) < 0.01f) return;
            appliedUiScale = scale;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(19f * scale), fontStyle = FontStyle.Bold, wordWrap = true };
            wrapStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = Mathf.RoundToInt(16f * scale), richText = true };
            statusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(15f * scale) };
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * scale);
            GUI.skin.button.fixedHeight = Mathf.Round(30f * scale);
        }

        private static Color ParseColor(string html, Color fallback)
        {
            return !string.IsNullOrWhiteSpace(html) && ColorUtility.TryParseHtmlString(html, out var color) ? color : fallback;
        }
    }
}
