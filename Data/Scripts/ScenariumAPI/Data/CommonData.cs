using System.Collections.Generic;

namespace ScenariumAPI.Data
{
    public class Vector3DData
    {
        public double X;
        public double Y;
        public double Z;

        public Vector3DData()
        {
        }

        public Vector3DData(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class LocalizedTextData
    {
        public string Key;
        public string Text;
    }

    public class RequirementData
    {
        public string RequirementId;
        public string DisplayName;
        public string Description;
        public string RequiredFactKey;
        public string RequiredFactValue;
        public string RequiredQuestId;
        public string RequiredScenarioId;
        public string RequiredNodeId;
        public string RequiredFactionTag;
        public bool Invert;
    }

    public class TagData
    {
        public string Key;
        public string Value;
    }

    public class ScenariumDataValidationResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();

        public void AddError(string message)
        {
            IsValid = false;
            Errors.Add(message);
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }
    }
}
