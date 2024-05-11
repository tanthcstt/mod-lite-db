# LiteDB.Studio (Modified)

## Note

The project may encounter errors if any directory contains Unicode characters.

### all syntax

## Insert Image
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\aaa.jpg)", mssv:22521300};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\Blue.png)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\dog.jpg)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\download.jpg)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\Green.jpg)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\Miner.png)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\Peasant.png)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\Swordman.png)"};
insert_img into TestImg values {aa:"Image(D:\\LiteDBTestImg\\or.jpg)"};

## select image by text description
select_image $ from TestImg where "An Apple";

select_image $ from TestImg where "An Apple" limit 2;





## test clear tmp data when delete/drop collection
delete TestImg where mssv=22521300;