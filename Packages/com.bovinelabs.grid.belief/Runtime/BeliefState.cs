using Unity.Collections;

namespace BovineLabs.Grid.Belief
{
    public struct BeliefState
    {
        public Grid2D Grid;
        public int LabelCount;
        public NativeArray<float> Unary;
        public NativeArray<float> Messages;
        public NativeArray<float> MessagesNext;
        public NativeArray<float> Belief;
        public NativeArray<float> Scratch;
    }
}