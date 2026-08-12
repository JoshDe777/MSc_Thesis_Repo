* Encoding: UTF-8.
DATASET ACTIVATE WideBackup.
COUNT ValidCount = V2 TO V19 (0 THRU 3).
EXECUTE.
GGRAPH
  /GRAPHDATASET NAME="graphdataset" VARIABLES=ValidCount
  /GRAPHSPEC SOURCE=INLINE.
BEGIN GPL
  SOURCE: s=userSource(id("graphdataset"))
  DATA: ValidCount=col(source(s), name("ValidCount"))
  GUIDE: axis(dim(1), label("State Change Count"), delta(1))
  GUIDE: axis(dim(2), label("Occurrences"))
  GUIDE: text.title(label("Distribution of the Amount of State Changes"))
  SCALE: linear(dim(1), min(3), max(19))
  ELEMENT: interval(position(summary.count(bin.rect(ValidCount, binStart(3.5), binWidth(1)))))
END GPL.