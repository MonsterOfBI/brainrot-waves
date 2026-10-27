using UnityEditor;
using UnityEngine;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Правила импорта скачанных ассетов, чтобы игра оставалась лёгкой (Яндекс Игры: до 100 МБ):
    /// звуки сжимаются в Vorbis и моно, текстуры частиц уменьшаются, у моделей отключено лишнее.
    /// </summary>
    public class ImportRules : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            bool ambientClip = assetPath.StartsWith("Assets/Resources/Ambient");
            if (!assetPath.StartsWith("Assets/Resources/Audio") && !assetPath.StartsWith("Assets/Resources/Music") && !ambientClip) return;
            var importer = (AudioImporter)assetImporter;
            bool music = assetPath.StartsWith("Assets/Resources/Music") || ambientClip;
            importer.forceToMono = true; // моно: в два раза легче, в браузерной игре разница не слышна
            importer.loadInBackground = music;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = ambientClip ? 0.25f : music ? 0.32f : 0.6f;
            if (music) settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = !music;
            importer.defaultSampleSettings = settings;
        }

        void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith("Assets/Resources/ParticleTex"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 128;
            }
            else if (assetPath.StartsWith("Assets/Resources/Decor") || assetPath.StartsWith("Assets/Resources/Weapons"))
            {
                importer.maxTextureSize = 256;
                importer.filterMode = FilterMode.Point; // палитра цветов Kenney: без размытия соседних цветов
                importer.mipmapEnabled = false;
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Resources/Decor") && !assetPath.StartsWith("Assets/Resources/Weapons")) return;
            var importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
        }
    }
}
