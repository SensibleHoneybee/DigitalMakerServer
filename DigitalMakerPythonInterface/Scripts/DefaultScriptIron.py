outputs_invoked = []

def output(output_to, parameter):
  outputs_invoked.append({"name" : json.dumps(str(output_to)), "parameter" : json.dumps(str(parameter))})

import sys
import json
def run_python_script():
{{{VARIABLE_DEFINITIONS}}}
{{{USER_CODE}}}
  result = ""
  digital_maker_local_variables = dict(locals())
  result = result + "{"
  digital_maker_first_variable = True
  for digital_maker_x in digital_maker_local_variables:
    if not digital_maker_first_variable:
      result = result + ","
    result = result + (json.dumps(str(digital_maker_x)))
    result = result + ":"
    result = result + (json.dumps(str(digital_maker_local_variables[digital_maker_x])))
    digital_maker_first_variable = False
  result = result + "}"
  return result