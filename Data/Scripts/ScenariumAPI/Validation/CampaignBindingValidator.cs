using System.Text;
using ScenariumAPI.Runtime;
using ScenariumAPI.Data;

namespace ScenariumAPI.Validation
{
    public class CampaignBindingValidator
    {
        public string ValidateMesBindings(CampaignRuntime runtime)
        {
            StringBuilder sb = new StringBuilder();

            if (runtime == null || runtime.Campaign == null)
            {
                sb.AppendLine("Binding validation failed: no campaign loaded.");
                return sb.ToString();
            }

            int checkedBindings = 0;
            int warnings = 0;

            foreach (var node in runtime.Campaign.ConquestNodes)
            {
                if (node == null)
                    continue;

                node.EnsureCollections();

                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    warnings++;
                    sb.AppendLine("WARN: Conquest node missing NodeId.");
                    continue;
                }

                foreach (var binding in node.Integrations)
                {
                    if (binding == null || !binding.Enabled)
                        continue;

                    checkedBindings++;

                    if (binding.IntegrationType == ScenariumIntegrationType.MES)
                    {
                        if (string.IsNullOrWhiteSpace(binding.BindingKey))
                        {
                            warnings++;
                            sb.AppendLine("WARN: " + node.NodeId + " MES binding has no BindingKey.");
                        }

                        if (string.IsNullOrWhiteSpace(binding.BindingValue))
                        {
                            warnings++;
                            sb.AppendLine("WARN: " + node.NodeId + " MES binding has no BindingValue.");
                        }

                        if (binding.TargetId != node.NodeId)
                        {
                            warnings++;
                            sb.AppendLine("WARN: " + node.NodeId + " MES binding TargetId does not match node.");
                        }
                    }
                }
            }

            sb.Insert(0, "Binding validation complete. Checked: " + checkedBindings + " Warnings: " + warnings + "\n");

            return sb.ToString();
        }
    }
}
