import sys
import re
from itertools import groupby
from typing import List

ListDividers = [
    ("Volt[mV]", 1000),
    ("VoltC1[mV]", 1000),
    ("VoltC2[mV]", 1000),
    ("VoltC3[mV]", 1000),
    ("VoltC4[mV]", 1000),
    ("Current[mA]", 1000),
]

def DividerNumber(file):
    # file ='roomtemp_rel_dis_rel.csv'
    table = open(file,'r', encoding='utf-8').readlines()
    
    IsHead = False
    IndxDividers = []
    for i, item in enumerate(table):
        item_split = re.split(',', item)
        if IsHead:
            for divider in IndxDividers:
                item_split[divider[1]] = str(float(item_split[divider[1]]) / divider[2])
            table[i] = ','.join(item_split)
            pass
        if item.find(ListDividers[0][0]) > 0:
            IsHead = True
            for divider in ListDividers:
                 IndxDividers.append( (divider[0], item_split.index(divider[0]), divider[1]) )
            pass
    file = "div." + file
    open(file,'w', encoding='utf-8').writelines(table)

if __name__ == "__main__":
    DividerNumber(sys.argv[1])
else:
    DividerNumber(sys.argv[1])
