/*
SelectLogInfoWithLogProperty.sql
*/
select
	info.seq, 
	info.exchange, 
	info.routing_key, 
	info.queue, 
	info.deliver_tag, 
	info.consumer_tag, 
	info.redelivered, 
	info.payload, 
	info.property_seq, 
	info.ins_date, 
	info.upd_date,
	prop.seq, 
	prop.content_type, 
	prop.content_encoding, 
	prop.delivery_mode, 
	prop.priority, 
	prop.correlation_id, 
	prop.reply_to, 
	prop.expiration, 
	prop.app_id, 
	prop.message_id, 
	prop.timestamp, 
	prop.type, 
	prop.user_id, 
	prop.cluster_id, 
	prop.headers, 
	prop.ins_date, 
	prop.upd_date
from 
	log_info info 
join 
	log_property prop 
on 
	info.property_seq = prop.seq
where info.ins_date between @begin_date and @end_date
and (info.routing_key = '' or info.routing_key like @routing_key)
order by info.seq desc
limit @limit
;