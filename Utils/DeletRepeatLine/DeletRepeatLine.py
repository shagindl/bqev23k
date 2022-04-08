import sys
from itertools import groupby

def DeletRepeatLine(file):
    # file ='roomtemp_rel_dis_rel.csv'
    uniqlines = open(file,'r', encoding='utf-8').readlines()
    uniqlines = [el for el, _ in groupby(uniqlines)]
    open(file,'w', encoding='utf-8').writelines(uniqlines)

if __name__ == "__main__":
    DeletRepeatLine(sys.argv[1])
