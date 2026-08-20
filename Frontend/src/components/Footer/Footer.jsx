import { Link } from 'react-router-dom';
import './Footer.css';

export default function Footer() {
  return (
    <footer className="footer">
      <div className="container">
        <div className="footer-grid">
          <div className="footer-brand-col">
            <Link to="/" className="footer-brand">
              <span className="brand-icon">🍕</span>
              <span className="brand-text">FoodRush</span>
            </Link>
            <p className="footer-tagline">
              Delivering happiness, one meal at a time. Experience the future of food delivery with FoodRush.
            </p>
          </div>

          <div className="footer-links-col">
            <h4 className="footer-heading">Explore</h4>
            <ul>
              <li><Link to="/restaurants">Restaurants</Link></li>
              <li><Link to="/register?role=Partner">Partner with Us</Link></li>
              <li><Link to="/register?role=DeliveryAgent">Deliver with Us</Link></li>
            </ul>
          </div>

          <div className="footer-links-col">
            <h4 className="footer-heading">Support</h4>
            <ul>
              <li><Link to="/help">Help Center</Link></li>
              <li><Link to="/contact">Contact Us</Link></li>
              <li><Link to="/help#ordering">FAQs</Link></li>
            </ul>
          </div>

          <div className="footer-links-col">
            <h4 className="footer-heading">Legal</h4>
            <ul>
              <li><a href="#">Privacy Policy</a></li>
              <li><a href="#">Terms of Service</a></li>
              <li><a href="#">Cookie Settings</a></li>
            </ul>
          </div>
        </div>

        <div className="footer-bottom">
          <p>© {new Date().getFullYear()} FoodRush. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
}
