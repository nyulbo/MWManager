/*
SelectLogInfo.sql
*/
select
	seq, 
	exchange, 
	routing_key, 
	queue, 
	deliver_tag, 
	consumer_tag, 
	redelivered, 
	payload, 
	property_seq, 
	ins_date, 
	upd_date
from 
	log_info
where ins_date between @begin_date and @end_date
and routing_key like @routing_key
order by seq desc
limit @limit
;