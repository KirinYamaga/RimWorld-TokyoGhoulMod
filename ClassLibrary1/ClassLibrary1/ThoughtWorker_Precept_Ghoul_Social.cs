using Verse;
using RimWorld;


namespace TokyoGhoulMod
{
    //Наследуем ThoughtWorker_Precept_Social для возможности изменения отношений 
    public class ThoughtWorker_Precept_Ghoul_Social : RimWorld.ThoughtWorker_Precept_Social
    {
        //Проверяем наличие на карте пешек с необходимым условием
        protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
        {
            //Находим пешку с геном гуля
            return otherPawn.IsGhoul();
        }
    }
}