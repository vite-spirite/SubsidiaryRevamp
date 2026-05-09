using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SubsidiaryRevamp
{
    public class SubsidiaryRevampBaheviour : ModBehaviour
    {

        private int _minSubsidiaryCreationCost = 1000000;
        private int _maxSubsidiaryCreationCost = 1000000 * 100;

        private MainBottomButton _bottomHudButton;

        private Dictionary<string, Component> _elements;

        public override void OnActivate()
        {
            DevConsole.Console.LogInfo("Subsidiary Revamp mod activated");
            DevConsole.Console.Log(ParentMod.ModPath);

        }

        public override void OnDeactivate()
        {
            DevConsole.Console.LogInfo("Subsidiary Revamp mod deactivated");

            if (GameSettings.Instance != null && HUD.Instance != null)
            {
                HUD.Instance.BottomButtons.Remove(_bottomHudButton);
            }
        }

        public void OnDestroy()
        {
            GameSettings.GameReady -= OnGameReady;
        }

        public void Start()
        {
            GameSettings.GameReady += OnGameReady;

            if (GameSettings.Instance != null && HUD.Instance != null)
            {
                DevConsole.Console.LogInfo("Game is already ready, adding bottom button");
                CreateBottomButton();
            }
        }

        private void OnGameReady(object sender, EventArgs e)
        {
            DevConsole.Console.LogInfo("Game is ready, adding bottom button");
            CreateBottomButton();
        }

        private void CreateBottomButton()
        {
            string[] marketplaceTranslation;
            Localization.CurrentTranslation.TryGetValue("Marketplace", out marketplaceTranslation);

            string category = marketplaceTranslation.FirstOrDefault() ?? "Marketplace";

            _bottomHudButton = HUD.Instance.AddBottomButton(category, "Create subsidiary", "Create a new subsidiary", ObjectDatabase.GetIcon("Skyskraper"));
            _bottomHudButton.Button.onClick.AddListener(() => {
                DevConsole.Console.Log("Button pressed");
                ShowCreateSubsidiaryPanel();
            });
        }

        private void ShowCreateSubsidiaryPanel()
        {
            try
            {
                _elements = WindowManager.GenerateUI(ParentMod.LoadFullXMLFile("ui/createSubsidiary.txt"), null, this);
            }
            catch
            {
                DevConsole.Console.LogError("Failed to load create subsidiary panel");
            }

            var budget = (Slider)_elements["budgetSlider"];
            var budgetLabel = (Text)_elements["selectedBudget"];
            var specializationDropdown = (GUICombobox)_elements["SpecializationSelect"];

            budget.value = 0.5f;
            int defaultPrice = (int)(_maxSubsidiaryCreationCost * budget.value) + _minSubsidiaryCreationCost;
            budgetLabel.text = $"Budget: {defaultPrice.ToString("C0")}";

            List<string> specializations = new List<string>();


            var companyTypes = GameData.AllCompanyTypes();
            foreach (var companyType in companyTypes)
            {
                var types = companyType.GetTypes();
                List<string> fullName = new List<string>();

                foreach (var item in types)
                {
                    //specializations.Add(Localization.LocSWFull(item.Key, item.Value.First()));
                    fullName.Add(Localization.LocSWFull(item.Key, item.Value.First()));
                }

                specializations.Add(string.Join(" ", fullName));
            }

            specializationDropdown.UpdateContent(specializations);


            budget.onValueChanged.AddListener((value) =>
            {
                UpdateBudgetLabel();
            });

            specializationDropdown.OnSelectedChanged.AddListener(() => { UpdateBudgetLabel(); });

        }

        public void CreateSubsidiary()
        {
            Text errorLabel = (Text)_elements["errorLabel"];

            CompanyType selectedType = GameData.AllCompanyTypes().GetAt(((GUICombobox)_elements["SpecializationSelect"]).Selected);

            if (selectedType == null)
            {
                errorLabel.text = "Invalid company type selected";
                return;
            }

            int budget = GetBudget();


            float avgQuality = BudgetToQuality(budget, selectedType);
            Company playerCompany = GameSettings.Instance.MyCompany;
            string subsidiaryName = ((InputField)_elements["companyName"]).text;

            if (subsidiaryName.Length <= 0)
            {
                //DevConsole.Console.LogError("Subsidiary name cannot be empty");
                errorLabel.text = "Subsidiary name cannot be empty";
                return;
            }

            if (playerCompany.Money < budget)
            {
                //DevConsole.Console.LogError("Not enough money to create subsidiary");
                errorLabel.text = "Not enough money to create subsidiary";
                return;
            }

            SimulatedCompany subsidiary = new SimulatedCompany(subsidiaryName, SDateTime.Now(), selectedType, selectedType.GetTypes(), avgQuality, MarketSimulation.Active);
            MarketSimulation.Active.AddCompany(subsidiary);

            subsidiary.MakeSubsidiary(playerCompany, SDateTime.Now());
            subsidiary.MakeTransaction(budget - subsidiary.Money, Company.TransactionCategory.Intercompany);

            foreach (var fans in playerCompany.GetSoftwarePop())
            {
                subsidiary.AddFans((int)(fans.Value * 0.5), fans.Key);
            }

            playerCompany.MakeTransaction(-budget, Company.TransactionCategory.Intercompany);
            ((GUIWindow)_elements["window"]).Close();
        }

        private float GetCostMultiplier(CompanyType type)
        {
            switch (type.Name.ToLower())
            {
                case "antivirus": return 0.5f;
                case "computer operating systems": return 2f;
                case "phone operating systems": return 2.5f;
                case "console operating systems": return 2.5f;
                case "game": return 1.5f;
                default: return 1f;
            }
        }

        private float BudgetToQuality(int budget, CompanyType type)
        {
            float multiplier = GetCostMultiplier(type);

            float min = (float)_minSubsidiaryCreationCost * multiplier;
            float max = (float)_maxSubsidiaryCreationCost * multiplier;

            float t = (float)Math.Sqrt((budget - min) / (max - min));
            return Mathf.Clamp(t, 0.1f, 0.9f);
        }

        private int GetBudget()
        {
            Slider slider = (Slider)_elements["budgetSlider"];
            GUICombobox specializationSelect = (GUICombobox)_elements["SpecializationSelect"];

            CompanyType selectedType = GameData.AllCompanyTypes().GetAt(specializationSelect.Selected);

            float multiplier = GetCostMultiplier(selectedType);
            float min = (_minSubsidiaryCreationCost * multiplier);
            float max = (_maxSubsidiaryCreationCost * multiplier);

            int realBudget = (int)(min + (max - min) * slider.value * slider.value);
            return realBudget;
        }

        private void UpdateBudgetLabel()
        {
            Text label = (Text)_elements["selectedBudget"];
            int budget = GetBudget();

            label.text = $"Budget: {budget.ToString("C0")}$";
        }
    }
}
