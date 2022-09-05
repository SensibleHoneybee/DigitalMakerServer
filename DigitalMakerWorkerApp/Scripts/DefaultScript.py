outputs_invoked = []

def my_function():
  print("Hello from a function")

import sys
import json
{{{VARIABLE_DEFINITIONS}}}
{{{USER_CODE}}}
digital_maker_local_variables = dict(locals())
sys.stdout.write("{")
digital_maker_first_variable = True
for digital_maker_x in digital_maker_local_variables:
  if not digital_maker_first_variable:
    sys.stdout.write(",")
  sys.stdout.write((json.dumps(str(digital_maker_x))))
  sys.stdout.write(":")
  sys.stdout.write((json.dumps(str(digital_maker_local_variables[digital_maker_x]))))
  digital_maker_first_variable = False
sys.stdout.write("}")
sys.stdout.flush()
sys.exit(0)