using System.Linq;
using UnityEngine;

namespace SubsidiaryRevamp
{
    public class Main : ModMeta
    {

        private SubsidiaryRevampBaheviour _behaviour;


        public override string Name => "Subsidiary Revamp";

        public override void Initialize(ModController.DLLMod parentMod)
        {
            _behaviour = parentMod.Behaviors.OfType<SubsidiaryRevampBaheviour>().First();
        }

        public override void ConstructOptionsScreen(RectTransform parent, bool inGame)
        {
            var label = WindowManager.SpawnLabel();
            label.text = "Create subsidiary";

            WindowManager.AddElementToElement(label.gameObject, parent.gameObject, new Rect(0, 0, 96, 32),
                new Rect(0, 0, 0, 0));
        }
    }
}
