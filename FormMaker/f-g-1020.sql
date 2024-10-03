INSERT INTO svod2.form(form,periodic,kind,formregulations,modeenter,modevertical,hassubject,hasfacility,hasterritory,name,formdate,since,changedate) VALUES(1020,1,1,5,2,1,1,0,0,'a','16.09.2024 0:00:00','16.09.2024 0:00:00','16.09.2024 0:00:00');
INSERT INTO svod2.formcolumn(form,formcolumn,name,summation,mandatory,copying,changedate) VALUES(1020,1,'%name',0,0,0,'16.09.2024 0:00:00');
INSERT INTO svod2.formcolumn(form,formcolumn,name,summation,mandatory,copying,changedate) VALUES(1020,2,'%code',0,0,0,'16.09.2024 0:00:00');
INSERT INTO svod2.formcolumn(form,formcolumn,name,type,precompute,summation,mandatory,copying,changedate) VALUES(1020,3,'aaa',1,'[3]/*y-*/',2,1,1,'16.09.2024 0:00:00');
INSERT INTO svod2.formrow(form,formrow,code,name,type,summation,mandatory,copying,changedate) VALUES(1020,1,'1','a',3,2,1,1,'16.09.2024 0:00:00');
