import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Package, Clock, MapPin, CreditCard, CheckCircle, XCircle, Truck } from 'lucide-react';
import api from '../services/api';
import { API_ENDPOINTS } from '../config/api';
import toast from 'react-hot-toast';
import './OrderDetailPage.css';

const statusSteps = ['Pending', 'Confirmed', 'Preparing', 'Ready', 'PickedUp', 'OutForDelivery', 'Delivered'];

export default function OrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  const fetchOrder = async () => {
    try {
      const { data } = await api.get(API_ENDPOINTS.orders.orderById(id));
      setOrder(data);
    } catch { toast.error('Failed to load order'); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchOrder(); }, [id]);

  const handleCancel = async () => {
    if (!window.confirm('Cancel this order?')) return;
    try {
      await api.post(API_ENDPOINTS.orders.orderCancel(id));
      toast.success('Order cancelled');
      fetchOrder();
    } catch (err) { toast.error(err.response?.data?.message || 'Cannot cancel'); }
  };

  if (loading) return <div className="od-page page-enter container" style={{display:'flex',justifyContent:'center',padding:'4rem 0'}}><div className="spinner"/></div>;
  if (!order) return <div className="od-page page-enter container" style={{textAlign:'center',padding:'4rem 0'}}><h2 className="headline-md">Order not found</h2><Link to="/orders" className="btn btn-outline" style={{marginTop:'1rem'}}>Back</Link></div>;

  const currentStep = statusSteps.findIndex(s => s.toLowerCase() === (order.status||'').toLowerCase());
  const isCancelled = (order.status||'').toLowerCase() === 'cancelled';
  const isDelivered = (order.status||'').toLowerCase() === 'delivered';

  return (
    <div className="od-page page-enter">
      <div className="container">
        <div className="od-header">
          <div><Link to="/orders" className="body-sm text-muted">← Back</Link><h1 className="headline-lg">Order #{(order.id||'').slice(-8).toUpperCase()}</h1></div>
          <span className={`badge ${isCancelled?'badge-error':isDelivered?'badge-success':'badge-primary'}`} style={{fontSize:14,padding:'0.5rem 1rem'}}>{order.status}</span>
        </div>
        {!isCancelled && (
          <div className="od-progress">
            {statusSteps.map((step,i) => (
              <div key={step} className={`od-step ${i<=currentStep?'completed':''} ${i===currentStep?'active':''}`}>
                <div className="od-step-dot">{i<currentStep?<CheckCircle size={16}/>:i===currentStep?<Truck size={16}/>:null}</div>
                <span className="od-step-label">{step}</span>
              </div>
            ))}
          </div>
        )}
        <div className="od-grid">
          <div className="od-section card"><div className="card-body">
            <h3 className="headline-sm" style={{marginBottom:'1rem'}}><Package size={18} style={{display:'inline',verticalAlign:-3,marginRight:8}}/>Items</h3>
            {(order.items||order.orderItems||[]).map((item,i) => (
              <div key={i} className="od-item"><div><span style={{fontWeight:600}}>{item.name||item.menuItemName}</span><span className="text-muted"> × {item.quantity}</span></div><span style={{fontWeight:600}}>₹{((item.price||0)*(item.quantity||1)).toFixed(2)}</span></div>
            ))}
            <div className="od-item total"><span style={{fontWeight:700}}>Total</span><span style={{fontWeight:700,fontSize:18}}>₹{(order.totalAmount||order.total||0).toFixed(2)}</span></div>
          </div></div>
          <div className="od-section card"><div className="card-body">
            <h3 className="headline-sm" style={{marginBottom:'1rem'}}>Details</h3>
            <div className="od-detail"><Clock size={16}/><div><span className="label-md">Ordered</span><p className="body-sm">{new Date(order.createdAt||order.orderDate).toLocaleString('en-IN')}</p></div></div>
            {order.deliveryAddress && <div className="od-detail"><MapPin size={16}/><div><span className="label-md">Address</span><p className="body-sm">{order.deliveryAddress}</p></div></div>}
            <div className="od-detail"><CreditCard size={16}/><div><span className="label-md">Payment</span><p className="body-sm">{order.paymentMethod||'Online'} — {order.paymentStatus||'Pending'}</p></div></div>
            {!isCancelled && !isDelivered && currentStep<=1 && <button className="btn btn-outline" style={{width:'100%',marginTop:'1rem',color:'var(--error)'}} onClick={handleCancel}><XCircle size={16}/> Cancel</button>}
          </div></div>
        </div>
      </div>
    </div>
  );
}
