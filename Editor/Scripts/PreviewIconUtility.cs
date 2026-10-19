using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CollisionBear.BearDataEditor {
    public static class PreviewIconUtility {
        private static readonly BindingFlags IconFieldLookupFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<Type, FieldInfo> CachedIconFieldsByType = new Dictionary<Type, FieldInfo>();

        private struct IconSource {
            public Texture SourceTexture;
            public Rect VisiblePixelsInSourceTexture;
        }

        private struct IconPlacement {
            public int SourcePixelX;
            public int SourcePixelY;
            public int SourcePixelWidth;
            public int SourcePixelHeight;
            public int DestinationX;
            public int DestinationY;
            public int DestinationWidth;
            public int DestinationHeight;
        }

        public static Texture2D Render(UnityEngine.Object asset, int previewWidth, int previewHeight) {
            if (!HasDrawablePreviewSize(previewWidth, previewHeight)) {
                return null;
            }

            if (!TryGetIconSource(asset, out var iconSource)) {
                return null;
            }

            var iconPlacement = CalculateIconPlacement(iconSource, previewWidth, previewHeight);
            return ComposePreviewTexture(iconSource, iconPlacement, previewWidth, previewHeight);
        }

        private static bool HasDrawablePreviewSize(int previewWidth, int previewHeight) {
            return previewWidth > 0 && previewHeight > 0;
        }

        private static bool TryGetIconSource(UnityEngine.Object asset, out IconSource iconSource) {
            iconSource = default;
            if (asset == null) {
                return false;
            }

            var iconField = GetCachedIconField(asset.GetType());
            if (iconField == null) {
                return false;
            }

            return TryReadIconSource(iconField.GetValue(asset), out iconSource);
        }

        internal static bool HasPreviewIconField(Type assetType) {
            return assetType != null && GetCachedIconField(assetType) != null;
        }

        private static FieldInfo GetCachedIconField(Type assetType) {
            if (CachedIconFieldsByType.TryGetValue(assetType, out var cachedIconField)) {
                return cachedIconField;
            }

            var iconField = FindIconField(assetType);
            CachedIconFieldsByType[assetType] = iconField;
            return iconField;
        }

        private static FieldInfo FindIconField(Type assetType) {
            for (var currentType = assetType; IsSearchableType(currentType); currentType = currentType.BaseType) {
                var declaredIconField = FindDeclaredIconField(currentType);
                if (declaredIconField != null) {
                    return declaredIconField;
                }
            }

            return null;
        }

        private static bool IsSearchableType(Type assetType) {
            return assetType != null && assetType != typeof(UnityEngine.Object);
        }

        private static FieldInfo FindDeclaredIconField(Type assetType) {
            foreach (var field in assetType.GetFields(IconFieldLookupFlags)) {
                if (field.IsDefined(typeof(PreviewIconAttribute), true)) {
                    return field;
                }
            }

            return null;
        }

        private static bool TryReadIconSource(object iconFieldValue, out IconSource iconSource) {
            if (TryReadSpriteIcon(iconFieldValue as Sprite, out iconSource)) {
                return true;
            }

            return TryReadTextureIcon(iconFieldValue as Texture2D, out iconSource);
        }

        private static bool TryReadSpriteIcon(Sprite sprite, out IconSource iconSource) {
            iconSource = default;
            if (!IsNativeUnityObjectAlive(sprite) || !TryCopySpritePixels(sprite, out var spriteTexture, out var visiblePixels)) {
                return false;
            }

            if (!TryGetReadableTextureSize(spriteTexture, out _, out _)) {
                return false;
            }

            iconSource.SourceTexture = spriteTexture;
            iconSource.VisiblePixelsInSourceTexture = visiblePixels;
            return true;
        }

        private static bool TryCopySpritePixels(Sprite sprite, out Texture2D spriteTexture, out Rect visiblePixels) {
            spriteTexture = null;
            visiblePixels = default;

            try {
                spriteTexture = sprite.texture;
                visiblePixels = sprite.textureRect;
            } catch (Exception spriteReadFailure) when (spriteReadFailure is MissingReferenceException || spriteReadFailure is UnityException) {
                return false;
            }

            return IsNativeUnityObjectAlive(spriteTexture) && visiblePixels.width > 0f && visiblePixels.height > 0f;
        }

        private static bool TryReadTextureIcon(Texture2D texture, out IconSource iconSource) {
            iconSource = default;
            if (!TryGetReadableTextureSize(texture, out var textureWidth, out var textureHeight)) {
                return false;
            }

            iconSource.SourceTexture = texture;
            iconSource.VisiblePixelsInSourceTexture = new Rect(0, 0, textureWidth, textureHeight);
            return true;
        }

        private static bool TryGetReadableTextureSize(Texture texture, out int textureWidth, out int textureHeight) {
            textureWidth = 0;
            textureHeight = 0;
            if (!IsNativeUnityObjectAlive(texture)) {
                return false;
            }

            try {
                textureWidth = texture.width;
                textureHeight = texture.height;
            } catch (Exception textureReadFailure) when (textureReadFailure is MissingReferenceException || textureReadFailure is UnityException) {
                return false;
            }

            return textureWidth > 0 && textureHeight > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsNativeUnityObjectAlive(UnityEngine.Object candidate) {
            return candidate != null;
        }

        private static IconPlacement CalculateIconPlacement(IconSource iconSource, int previewWidth, int previewHeight) {
            var sourcePixelRect = ClampSourcePixelRect(iconSource.VisiblePixelsInSourceTexture, iconSource.SourceTexture);

            return new IconPlacement {
                SourcePixelX = Mathf.FloorToInt(sourcePixelRect.x),
                SourcePixelY = Mathf.FloorToInt(sourcePixelRect.y),
                SourcePixelWidth = Mathf.RoundToInt(sourcePixelRect.width),
                SourcePixelHeight = Mathf.RoundToInt(sourcePixelRect.height),
                DestinationX = 0,
                DestinationY = 0,
                DestinationWidth = previewWidth,
                DestinationHeight = previewHeight
            };
        }

        private static Rect ClampSourcePixelRect(Rect visiblePixels, Texture sourceTexture) {
            var sourcePixelX = Mathf.Clamp(Mathf.FloorToInt(visiblePixels.x), 0, sourceTexture.width - 1);
            var sourcePixelY = Mathf.Clamp(Mathf.FloorToInt(visiblePixels.y), 0, sourceTexture.height - 1);
            var sourcePixelWidth = Mathf.Clamp(Mathf.RoundToInt(visiblePixels.width), 1, sourceTexture.width - sourcePixelX);
            var sourcePixelHeight = Mathf.Clamp(Mathf.RoundToInt(visiblePixels.height), 1, sourceTexture.height - sourcePixelY);
            return new Rect(sourcePixelX, sourcePixelY, sourcePixelWidth, sourcePixelHeight);
        }

        private static Texture2D ComposePreviewTexture(IconSource iconSource, IconPlacement iconPlacement, int previewWidth, int previewHeight) {
            var previouslyActiveRenderTexture = RenderTexture.active;
            var temporaryRenderTextures = new List<RenderTexture>();

            try {
                var scaledIcon = CreateScaledIcon(iconSource.SourceTexture, iconPlacement, temporaryRenderTextures);
                return ReadPreviewTexture(scaledIcon, previewWidth, previewHeight);
            }
            finally {
                RenderTexture.active = previouslyActiveRenderTexture;
                ReleaseRenderTextures(temporaryRenderTextures);
            }
        }

        private static RenderTexture CreateScaledIcon(Texture sourceTexture, IconPlacement iconPlacement, List<RenderTexture> temporaryRenderTextures) {
            if (CoversEntireSourceTexture(iconPlacement, sourceTexture)) {
                return TrackRenderTexture(ScaleTexture(sourceTexture, iconPlacement.DestinationWidth, iconPlacement.DestinationHeight), temporaryRenderTextures);
            }

            var croppedIcon = CropIconFromSourceTexture(sourceTexture, iconPlacement, temporaryRenderTextures);
            return TrackRenderTexture(ScaleTexture(croppedIcon, iconPlacement.DestinationWidth, iconPlacement.DestinationHeight), temporaryRenderTextures);
        }

        private static bool CoversEntireSourceTexture(IconPlacement iconPlacement, Texture sourceTexture) {
            return iconPlacement.SourcePixelX == 0
                && iconPlacement.SourcePixelY == 0
                && iconPlacement.SourcePixelWidth == sourceTexture.width
                && iconPlacement.SourcePixelHeight == sourceTexture.height;
        }

        private static RenderTexture CropIconFromSourceTexture(Texture sourceTexture, IconPlacement iconPlacement, List<RenderTexture> temporaryRenderTextures) {
            var decodedSource = TrackRenderTexture(DecodeSourceTexture(sourceTexture), temporaryRenderTextures);
            return TrackRenderTexture(CopySourcePixelRect(decodedSource, iconPlacement), temporaryRenderTextures);
        }

        private static RenderTexture DecodeSourceTexture(Texture sourceTexture) {
            var decodedSource = CreateRenderTexture(sourceTexture.width, sourceTexture.height);
            Graphics.Blit(sourceTexture, decodedSource);
            return decodedSource;
        }

        private static RenderTexture CopySourcePixelRect(RenderTexture decodedSource, IconPlacement iconPlacement) {
            var croppedIcon = CreateRenderTexture(iconPlacement.SourcePixelWidth, iconPlacement.SourcePixelHeight);
            Graphics.CopyTexture(
                decodedSource,
                0,
                0,
                iconPlacement.SourcePixelX,
                iconPlacement.SourcePixelY,
                iconPlacement.SourcePixelWidth,
                iconPlacement.SourcePixelHeight,
                croppedIcon,
                0,
                0,
                0,
                0);
            return croppedIcon;
        }

        private static RenderTexture ScaleTexture(Texture sourceTexture, int scaledWidth, int scaledHeight) {
            var scaledTexture = CreateRenderTexture(scaledWidth, scaledHeight);
            Graphics.Blit(sourceTexture, scaledTexture);
            return scaledTexture;
        }

        private static Texture2D ReadPreviewTexture(RenderTexture preview, int previewWidth, int previewHeight) {
            var previewTexture = new Texture2D(previewWidth, previewHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = preview;
            GL.Flush();
            previewTexture.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
            previewTexture.Apply();
            return previewTexture;
        }

        private static RenderTexture TrackRenderTexture(RenderTexture renderTexture, List<RenderTexture> temporaryRenderTextures) {
            temporaryRenderTextures.Add(renderTexture);
            return renderTexture;
        }

        private static void ReleaseRenderTextures(List<RenderTexture> temporaryRenderTextures) {
            foreach (var renderTexture in temporaryRenderTextures) {
                ReleaseRenderTexture(renderTexture);
            }
        }

        private static RenderTexture CreateRenderTexture(int textureWidth, int textureHeight) {
            var renderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            renderTexture.Create();
            return renderTexture;
        }

        private static void ReleaseRenderTexture(RenderTexture renderTexture) {
            if (renderTexture == null) {
                return;
            }

            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }
}
