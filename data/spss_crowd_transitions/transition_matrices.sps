* Encoding: UTF-8.
DATASET ACTIVATE WideBackup.
DATASET COPY WorkingLong.
DATASET ACTIVATE WorkingLong.

VARSTOCASES
  /MAKE TargetValue FROM V2 TO V19
  /INDEX Seq(18)
  /KEEP ID.

SORT CASES BY ID Seq.

COMPUTE SourceValue = LAG(TargetValue).
COMPUTE PrevID = LAG(ID).
EXECUTE.

COMPUTE ValidPair = (ID = PrevID) AND (TargetValue >= 0) AND (SourceValue >= 0).
EXECUTE.

TEMPORARY.
SELECT IF (ValidPair = 1).
CROSSTABS
  /TABLES=SourceValue BY TargetValue
  /CELLS=COUNT.

TEMPORARY.
SELECT IF (ValidPair = 1).
CROSSTABS
  /TABLES=SourceValue BY TargetValue
  /CELLS=ROW.
