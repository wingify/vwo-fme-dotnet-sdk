#pragma warning disable 1587
/**
 * Copyright 2024 Wingify Software Pvt. Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma warning restore 1587

using System.Collections.Generic;

namespace VWOFmeSdk.Models.User
{
    public class GetFlag
    {
        private bool isEnabled = false;
        private List<Variable> variables = new List<Variable>();

        public bool IsEnabled()
        {
            return isEnabled;
        }

        public void SetIsEnabled(bool value)
        {
            isEnabled = value;
        }

        public List<Variable> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        public object GetVariable(string key, object defaultValue)
        {
            foreach (var variable in Variables)
            {
                if (variable.Key.Equals(key))
                {
                    return variable.Value;
                }
            }
            return defaultValue;
        }

        public List<Dictionary<string, object>> GetVariables()
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var variable in Variables)
            {
                result.Add(ConvertVariableModelToMap(variable));
            }
            return result;
        }

        private Dictionary<string, object> ConvertVariableModelToMap(Variable variableModel)
        {
            var map = new Dictionary<string, object>();
            map["key"] = variableModel.Key;
            map["value"] = variableModel.Value;
            map["type"] = variableModel.Type;
            map["id"] = variableModel.Id;
            return map;
        }
    }

}
