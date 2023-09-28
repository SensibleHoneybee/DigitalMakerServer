outputs_invoked = []

def output(output_to, parameter):
  outputs_invoked.append({"name" : json.dumps(str(output_to)), "parameter" : json.dumps(str(parameter))})

import sys
import json
{{{USER_CODE}}}