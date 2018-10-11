import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

// declare the leaflet variable
declare let L;
declare let tileLayer;

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'angular-leaflet';

  map: any;
  restItems: any;
  //restItemsUrl = 'https://public-api.wordpress.com/rest/v1.1/sites/vocon-it.com/posts';
  restItemsUrl = 'api/device';
  
  constructor(private http: HttpClient) { }

  // Read all REST Items
  getRestItems(): void {
    this.restItemsServiceGetRestItems()
      .subscribe(
        restItems => {
          this.restItems = restItems;
          console.log(this.restItems);
        }
      )
  }

  // Rest Items Service: Read all REST Items
  restItemsServiceGetRestItems() {
    return this.http
      .get<any[]>(this.restItemsUrl)
      .pipe(map(data => data));
  }

  initMap() {
    //Create and render the map
    const map = L.map('map').setView([47.5952, -122.3316], 16);

    L.tileLayer('assets/tiles/{z}/{x}/{y}.png', {
      cursor: true,
      minZoom: 16,
      maxZoom: 19,
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
  }

  ngOnInit() {

    //Get the device list from the rest API
    this.getRestItems();

    this.initMap();
  }

}

