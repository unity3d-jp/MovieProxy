namespace UnityEngine.StreamingImageSequence {

    [System.Serializable]
    public struct ImageDimensionInt {
        public int Width;
        public int Height;

        public float CalculateRatio() {
            return Width / (float) Height;
        }

    }

} //end namespace