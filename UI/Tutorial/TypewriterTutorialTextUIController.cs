using TMPro;
using UnityEngine;

namespace UnityCommonEx
{
    public class TypewriterTutorialTextUIController : TutorialTextUIController
    {
        public float CharacterJumpHeight = 24f;
        public float CharacterJumpDuration = 0.18f;
        public float CharacterInterval = 0.05f;
        [Min(1f)]
        [Tooltip("Multiplier applied to reveal interval for non-ASCII characters like Chinese.")]
        public float NonAsciiIntervalMultiplier = 2f;

        string currentText = string.Empty;
        float animationTime;
        bool isAnimating;
        int currentVisibleCharacters;
        float totalAnimationDuration;
        float[] characterRevealTimes;
        TMP_MeshInfo[] cachedMeshInfo;

        protected override void OnDeactivate()
        {
            StopAnimation();
            base.OnDeactivate();
        }

        protected override void OnRelease()
        {
            StopAnimation();
            base.OnRelease();
        }

        public override void SetText(string text)
        {
            if (Text == null)
            {
                return;
            }

            StopAnimation();

            currentText = text ?? string.Empty;
            Text.text = currentText;
            Text.maxVisibleCharacters = 0;
            Text.ForceMeshUpdate();
            currentVisibleCharacters = 0;

            if (string.IsNullOrEmpty(currentText) || Text.textInfo.characterCount == 0)
            {
                cachedMeshInfo = null;
                characterRevealTimes = null;
                totalAnimationDuration = 0f;
                Text.maxVisibleCharacters = int.MaxValue;
                return;
            }

            BuildCharacterRevealTimes(Text.textInfo);
            cachedMeshInfo = Text.textInfo.CopyMeshInfoVertexData();
            animationTime = 0f;
            isAnimating = true;
        }

        public override void ClearText()
        {
            StopAnimation(false);
            currentText = string.Empty;
            currentVisibleCharacters = 0;
            totalAnimationDuration = 0f;
            characterRevealTimes = null;
            cachedMeshInfo = null;
            base.ClearText();
        }

        void Update()
        {
            if (!isAnimating || Text == null)
            {
                return;
            }

            animationTime += Time.deltaTime;
            UpdateAnimatedText();
        }

        void StopAnimation(bool keepCompletedText = true)
        {
            if (!isAnimating)
            {
                return;
            }

            isAnimating = false;
            if (keepCompletedText && Text != null)
            {
                Text.maxVisibleCharacters = int.MaxValue;
                Text.ForceMeshUpdate();
            }
            cachedMeshInfo = null;
        }

        void UpdateAnimatedText()
        {
            TMP_TextInfo textInfo = Text.textInfo;
            int characterCount = textInfo.characterCount;
            if (characterCount == 0)
            {
                StopAnimation(false);
                return;
            }

            if (characterRevealTimes == null || characterRevealTimes.Length != characterCount)
            {
                BuildCharacterRevealTimes(textInfo);
            }

            int targetVisibleCharacters = GetVisibleCharacterCount(animationTime, characterCount);

            if (targetVisibleCharacters != currentVisibleCharacters || cachedMeshInfo == null)
            {
                currentVisibleCharacters = targetVisibleCharacters;
                Text.maxVisibleCharacters = currentVisibleCharacters;
                Text.ForceMeshUpdate();
                textInfo = Text.textInfo;
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
            }

            if (cachedMeshInfo == null)
            {
                return;
            }

            for (int i = 0; i < textInfo.meshInfo.Length && i < cachedMeshInfo.Length; i++)
            {
                Vector3[] sourceVertices = cachedMeshInfo[i].vertices;
                Vector3[] destinationVertices = textInfo.meshInfo[i].vertices;
                int copyLength = Mathf.Min(sourceVertices.Length, destinationVertices.Length);
                for (int j = 0; j < copyLength; j++)
                {
                    destinationVertices[j] = sourceVertices[j];
                }
            }

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
                if (!characterInfo.isVisible)
                {
                    continue;
                }

                float charStartTime = characterRevealTimes != null && i < characterRevealTimes.Length
                    ? characterRevealTimes[i]
                    : 0f;
                float charElapsed = animationTime - charStartTime;
                if (charElapsed < 0f || CharacterJumpDuration <= 0f || charElapsed > CharacterJumpDuration)
                {
                    continue;
                }

                float normalized = Mathf.Clamp01(charElapsed / CharacterJumpDuration);
                float offsetY = Mathf.Sin(normalized * Mathf.PI) * CharacterJumpHeight;
                int materialIndex = characterInfo.materialReferenceIndex;
                int vertexIndex = characterInfo.vertexIndex;
                Vector3 offset = new Vector3(0f, offsetY, 0f);
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                vertices[vertexIndex] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            if (animationTime >= totalAnimationDuration)
            {
                StopAnimation();
            }
        }

        void BuildCharacterRevealTimes(TMP_TextInfo textInfo)
        {
            int characterCount = textInfo.characterCount;
            if (characterCount <= 0)
            {
                characterRevealTimes = null;
                totalAnimationDuration = 0f;
                return;
            }

            characterRevealTimes = new float[characterCount];
            characterRevealTimes[0] = 0f;
            for (int i = 1; i < characterCount; i++)
            {
                characterRevealTimes[i] = characterRevealTimes[i - 1] + GetCharacterInterval(textInfo.characterInfo[i].character);
            }

            totalAnimationDuration = characterRevealTimes[characterCount - 1] + Mathf.Max(0f, CharacterJumpDuration);
        }

        int GetVisibleCharacterCount(float elapsedTime, int characterCount)
        {
            if (characterCount <= 0)
            {
                return 0;
            }

            if (CharacterInterval <= 0f)
            {
                return characterCount;
            }

            int visibleCount = 0;
            while (visibleCount < characterCount && characterRevealTimes != null && elapsedTime >= characterRevealTimes[visibleCount])
            {
                visibleCount++;
            }
            return visibleCount;
        }

        float GetCharacterInterval(char character)
        {
            float baseInterval = Mathf.Max(0f, CharacterInterval);
            if (baseInterval <= 0f)
            {
                return 0f;
            }

            return IsAsciiCharacter(character) ? baseInterval : baseInterval * Mathf.Max(1f, NonAsciiIntervalMultiplier);
        }

        bool IsAsciiCharacter(char character)
        {
            return character <= 0x7F;
        }
    }
}
